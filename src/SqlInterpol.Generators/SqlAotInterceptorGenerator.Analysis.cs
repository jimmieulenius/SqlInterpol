using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqlInterpol.Generators;

public partial class SqlAotInterceptorGenerator
{
    private static readonly ConcurrentDictionary<string, Regex> _aliasRegexCache = new(StringComparer.OrdinalIgnoreCase);

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

    private static SqlAotAnalysisResult AnalyzeContents(InterpolatedStringExpressionSyntax interpolatedString, CompileTimeQueryContext queryContext)
    {
        var result = new SqlAotAnalysisResult();
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
                        if (i + 1 < contents.Count && contents[i + 1] is InterpolationSyntax nextHole)
                        {
                            ExpressionSyntax nextExpr = nextHole.Expression;
                            string? nextExplicitMode = null;
                            
                            // Unwrap the extension method to see if the hole after "AS" is a known, safe entity
                            UnwrapRenderExtension(ref nextExpr, ref nextExplicitMode);
                            
                            bool nextIsEntity = nextExpr is IdentifierNameSyntax id && queryContext.Entities.ContainsKey(id.Identifier.Text);
                            bool nextIsProperty = nextExpr is MemberAccessExpressionSyntax propMa && propMa.Expression is IdentifierNameSyntax propId && queryContext.Entities.ContainsKey(propId.Identifier.Text);
                            bool nextIsLiteralAlias = nextExpr is LiteralExpressionSyntax lit && lit.Kind() == SyntaxKind.StringLiteralExpression;
                            
                            // Only trigger JIT fallback if the hole after AS is a completely dynamic unknown variable
                            if (!nextIsEntity && !nextIsProperty && !nextIsLiteralAlias)
                            {
                                result.HasHoleAfterAs = true;
                            }
                        }
                    }
                }

                if (_returningRegex.IsMatch(StripSqlStringsAndComments(val))) result.HasReturning = true;
                if (_dmlQueryRegex.IsMatch(StripSqlStringsAndComments(val))) result.IsDmlQuery = true;
                if (_setOperationRegex.IsMatch(StripSqlStringsAndComments(val))) result.HasSetOperation = true;
                // Only treat "...) AS {hole}" patterns as unconsumable. Plain literal aliases like
                // "...) AS CategoryTotal" are safe for AOT and should not trigger fallback.
                // Also exempt ") AS {{entity:alias}}" — the emitter knows how to emit a quoted
                // alias for entity holes with the :alias format, so this is fully AOT-safe.
                if (_unconsumableAliasRegex.IsMatch(val))
                {
                    var trimmedAs = val.TrimEnd();
                    bool endsWithAs = trimmedAs.EndsWith(SqlKeyword.As.Value, StringComparison.OrdinalIgnoreCase)
                        && (trimmedAs.Length == SqlKeyword.As.Value.Length
                            || !char.IsLetterOrDigit(trimmedAs[trimmedAs.Length - (SqlKeyword.As.Value.Length + 1)]));
                    if (endsWithAs && i + 1 < contents.Count && contents[i + 1] is InterpolationSyntax nextUnconsumableHole)
                    {
                        ExpressionSyntax nextUncExpr = nextUnconsumableHole.Expression;
                        string? nextUncExtMode = null;
                        UnwrapRenderExtension(ref nextUncExpr, ref nextUncExtMode);
                        string? nextUncFmt = nextUncExtMode ?? nextUnconsumableHole.FormatClause?.FormatStringToken.ValueText;

                        // Safe: the hole is an entity with :alias format — the emitter handles this.
                        bool nextIsEntityAliasHole =
                            string.Equals(nextUncFmt, "alias", StringComparison.OrdinalIgnoreCase) &&
                            nextUncExpr is IdentifierNameSyntax nextUncId &&
                            queryContext.Entities.ContainsKey(nextUncId.Identifier.Text);

                        if (!nextIsEntityAliasHole)
                        {
                            result.HasUnconsumableAlias = true;
                        }
                    }
                }
                if (_windowFunctionRegex.IsMatch(StripSqlStringsAndComments(val))) result.HasWindowFunction = true;
                if (_upsertRegex.IsMatch(StripSqlStringsAndComments(val))) result.HasUpsert = true;

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
                ExpressionSyntax baseExpr = interpolation.Expression;
                string? explicitExtensionMode = null;

                UnwrapRenderExtension(ref baseExpr, ref explicitExtensionMode);

                string? format = explicitExtensionMode ?? interpolation.FormatClause?.FormatStringToken.ValueText;
                bool isLiteralStringHole = baseExpr is LiteralExpressionSyntax literalExpr && literalExpr.Kind() == SyntaxKind.StringLiteralExpression;

                bool followsAsKeyword = false;
                if (i > 0 && contents[i - 1] is InterpolatedStringTextSyntax prevText)
                {
                    var trimmedPrev = prevText.TextToken.ValueText.TrimEnd();
                    followsAsKeyword = trimmedPrev.EndsWith(SqlKeyword.As.Value, StringComparison.OrdinalIgnoreCase)
                        && (trimmedPrev.Length == SqlKeyword.As.Value.Length
                            || !char.IsLetterOrDigit(trimmedPrev[trimmedPrev.Length - (SqlKeyword.As.Value.Length + 1)]));
                }
                bool isLiteralAliasHole = isLiteralStringHole && (followsAsKeyword || string.Equals(format, "alias", StringComparison.OrdinalIgnoreCase));

                bool isEntity = baseExpr is IdentifierNameSyntax ident &&
                                queryContext.Entities.ContainsKey(ident.Identifier.Text);

                bool isProperty = false;
                if (baseExpr is MemberAccessExpressionSyntax propMemberAccess &&
                    propMemberAccess.Expression is IdentifierNameSyntax ident2 &&
                    queryContext.Entities.ContainsKey(ident2.Identifier.Text))
                {
                    isProperty = true;
                }
                else if (baseExpr is InvocationExpressionSyntax inv &&
                         inv.Expression is MemberAccessExpressionSyntax invMa &&
                         invMa.Name.Identifier.Text == "Column" &&
                         invMa.Expression is IdentifierNameSyntax invIdent &&
                         queryContext.Entities.ContainsKey(invIdent.Identifier.Text) &&
                         inv.ArgumentList.Arguments.Count == 1 &&
                         inv.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax)
                {
                    isProperty = true;
                }

                if (!isEntity && !isProperty && !isLiteralAliasHole)
                {
                    result.HasParameterHoles = true;

                    // Query fragment variables (ISqlQuery<T>) require runtime rendering.
                    if (baseExpr is IdentifierNameSyntax fragIdent &&
                        queryContext.QueryFragmentVariables.Contains(fragIdent.Identifier.Text))
                    {
                        result.HasComplexDynamicHoles = true;
                    }
                    else if (baseExpr is InvocationExpressionSyntax || baseExpr is MemberAccessExpressionSyntax)
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
                                // Only record if the alias is a valid SQL identifier (guards against CTE `{{entity}} AS (` patterns)
                                if (!string.IsNullOrEmpty(cleanAlias) && (char.IsLetter(cleanAlias[0]) || cleanAlias[0] == '_'))
                                {
                                    var aliasRegex = _aliasRegexCache.GetOrAdd(
                                        cleanAlias,
                                        static alias => new Regex(
                                            @"[ \t]*\b" + SqlKeyword.As.Value + @"\s+\[?" + Regex.Escape(alias) + @"\]?\b",
                                            RegexOptions.IgnoreCase | RegexOptions.Compiled));

                                    result.ReplacementForNextText[i] = aliasRegex.Replace(rawText, "", 1);

                                    if (isEntity) result.InlineAliases[((IdentifierNameSyntax)baseExpr).Identifier.Text] = cleanAlias;
                                    else if (isProperty) result.InlinePropertyAliases[i] = cleanAlias;
                                }
                            }
                            else if (isEntity && i + 2 < contents.Count && contents[i + 2] is InterpolationSyntax nextHole)
                            {
                                // Pattern: {{entity}} AS {{"literal"}} — alias is in the next string-literal hole.
                                ExpressionSyntax nextExpr = nextHole.Expression;
                                string? nextFmt = null;
                                UnwrapRenderExtension(ref nextExpr, ref nextFmt);
                                if (nextExpr is LiteralExpressionSyntax litAlias && litAlias.Kind() == SyntaxKind.StringLiteralExpression)
                                {
                                    string literalAlias = litAlias.Token.ValueText;
                                    result.InlineAliasesFromHoles[((IdentifierNameSyntax)baseExpr).Identifier.Text] = literalAlias;
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
}