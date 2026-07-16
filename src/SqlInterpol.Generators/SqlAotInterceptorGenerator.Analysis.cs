using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqlInterpol.Generators;

public partial class SqlAotInterceptorGenerator
{
    private static readonly Regex _returningRegex = new(
        $@"\b{SqlKeyword.Returning.Value}\b", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _dmlQueryRegex = new(
        $@"\b({SqlKeyword.Insert.Value}|{SqlKeyword.Update.Value}|{SqlKeyword.Delete.Value})\b", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _setOperationRegex = new(
        $@"\b({SqlKeyword.Intersect.Value}|{SqlKeyword.Union.Value}|{SqlKeyword.Except.Value})\b", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _unconsumableAliasRegex = new(
        $@"[)\]]\s*\b{SqlKeyword.As.Value}\b", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _windowFunctionRegex = new(
        $@"\b{SqlKeyword.Over.Value}\s*\(", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _upsertRegex = new(
        $@"\b(ON\s+CONFLICT|ON\s+DUPLICATE|{SqlKeyword.Merge.Value})\b", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static AotAnalysisResult AnalyzeContents(InterpolatedStringExpressionSyntax interpolatedString, CompileTimeQueryContext queryContext)
    {
        var result = new AotAnalysisResult();
        var contents = interpolatedString.Contents;

        for (int i = 0; i < contents.Count; i++)
        {
            if (contents[i] is InterpolatedStringTextSyntax textSyntax)
            {
                var val = textSyntax.TextToken.ValueText;
                var upperText = val.ToUpperInvariant();

                if (upperText.Contains(SqlKeyword.As.Value))
                    result.HasAsKeywordOrAlias = true;

                var trimmedEnd = val.TrimEnd();
                if (trimmedEnd.EndsWith(SqlKeyword.As.Value, StringComparison.OrdinalIgnoreCase))
                {
                    if (trimmedEnd.Length == SqlKeyword.As.Value.Length || !char.IsLetterOrDigit(trimmedEnd[trimmedEnd.Length - (SqlKeyword.As.Value.Length + 1)]))
                    {
                        if (i + 1 < contents.Count && contents[i + 1] is InterpolationSyntax)
                            result.HasHoleAfterAs = true;
                    }
                }

                if (_returningRegex.IsMatch(val)) result.HasReturning = true;
                if (_dmlQueryRegex.IsMatch(val)) result.IsDmlQuery = true;
                if (_setOperationRegex.IsMatch(val)) result.HasSetOperation = true;
                if (_unconsumableAliasRegex.IsMatch(val)) result.HasUnconsumableAlias = true;
                if (_windowFunctionRegex.IsMatch(val)) result.HasWindowFunction = true;
                if (_upsertRegex.IsMatch(val)) result.HasUpsert = true;

                int maxIdx = -1;
                SqlKeyword? matchedKeyword = null; // FIX CS8600: Explicit nullable annotation

                foreach (var kw in SqlKeyword.AllOrdered)
                {
                    if (!kw.IsClause) continue;

                    int idx = upperText.LastIndexOf(kw.Value);
                    if (idx > maxIdx)
                    {
                        maxIdx = idx;
                        matchedKeyword = kw;
                    }
                }

                if (matchedKeyword != null)
                {
                    result.PrePassClause = matchedKeyword.ClauseGroup;
                }
            }

            if (contents[i] is InterpolationSyntax interpolation)
            {
                string? format = interpolation.FormatClause?.FormatStringToken.ValueText;

                bool isEntity = interpolation.Expression is IdentifierNameSyntax ident &&
                                queryContext.Entities.ContainsKey(ident.Identifier.Text);

                bool isProperty = false;

                if (interpolation.Expression is MemberAccessExpressionSyntax propMemberAccess &&
                    propMemberAccess.Expression is IdentifierNameSyntax ident2 &&
                    queryContext.Entities.ContainsKey(ident2.Identifier.Text))
                {
                    isProperty = true;
                }
                else if (interpolation.Expression is InvocationExpressionSyntax inv &&
                         inv.Expression is MemberAccessExpressionSyntax invMa &&
                         invMa.Name.Identifier.Text == "Column" &&
                         invMa.Expression is IdentifierNameSyntax invIdent &&
                         queryContext.Entities.ContainsKey(invIdent.Identifier.Text) &&
                         inv.ArgumentList.Arguments.Count == 1 &&
                         inv.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax)
                {
                    isProperty = true;
                }

                if (!isEntity && !isProperty)
                {
                    result.HasParameterHoles = true;

                    if (interpolation.Expression is InvocationExpressionSyntax ||
                         interpolation.Expression is MemberAccessExpressionSyntax)
                    {
                        result.HasComplexDynamicHoles = true;
                    }

                    if (result.PrePassClause == SqlKeyword.OrderBy.Value || result.PrePassClause == SqlKeyword.GroupBy.Value)
                    {
                        result.HasComplexDynamicHoles = true;
                    }
                }

                if (!string.IsNullOrEmpty(format)) continue;

                if (isEntity || isProperty)
                {
                    if (i + 1 < contents.Count && contents[i + 1] is InterpolatedStringTextSyntax nextText)
                    {
                        var rawText = nextText.TextToken.ValueText;
                        string rawTrimmed = rawText.TrimStart(' ', '\r', '\n', '\t');

                        if (rawTrimmed.StartsWith($"{SqlKeyword.As.Value} ", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = rawTrimmed.Substring(SqlKeyword.As.Value.Length + 1).TrimStart().Split(new[] { ' ', '\r', '\n', '\t', ',', ')' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0)
                            {
                                string cleanAlias = parts[0].Trim('[', ']', '"', '\'', '`');
                                var dynamicAliasRegex = new Regex(
                                    @"[ \t]*\b" + SqlKeyword.As.Value + @"\s+\[?" + Regex.Escape(cleanAlias) + @"\]?\b", 
                                    RegexOptions.IgnoreCase);
                                
                                result.ReplacementForNextText[i] = dynamicAliasRegex.Replace(rawText, "", 1);

                                if (isEntity) result.InlineAliases[((IdentifierNameSyntax)interpolation.Expression).Identifier.Text] = cleanAlias;
                                else if (isProperty) result.InlinePropertyAliases[i] = cleanAlias;
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
}