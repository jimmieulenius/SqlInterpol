using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqlInterpol.Generators;

public partial class SqlAotInterceptorGenerator
{
    private static string Escape(string s) => s.Replace("\"", "\\\"");

    /// <summary>
    /// Replaces content inside SQL string literals (<c>'...'</c>) and comments
    /// (<c>-- ...</c> and <c>/* ... */</c>) with spaces so that keyword regexes
    /// do not fire on non-structural text (e.g., <c>RETURNING</c> inside a string).
    /// </summary>
    private static string StripSqlStringsAndComments(string text)
    {
        char[] result = text.ToCharArray();
        int i = 0;
        while (i < text.Length)
        {
            if (text[i] == '\'')
            {
                // SQL single-quoted string: skip until closing ', handling '' escapes
                result[i++] = ' ';
                while (i < text.Length)
                {
                    if (text[i] == '\'')
                    {
                        result[i++] = ' ';
                        if (i < text.Length && text[i] == '\'') // escaped ''
                            result[i++] = ' ';
                        else
                            break;
                    }
                    else
                    {
                        result[i++] = ' ';
                    }
                }
            }
            else if (i + 1 < text.Length && text[i] == '-' && text[i + 1] == '-')
            {
                // Line comment: skip to end of line, preserving \n for clause detection
                while (i < text.Length && text[i] != '\n')
                    result[i++] = ' ';
            }
            else if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '*')
            {
                // Block comment: skip to */
                result[i++] = ' ';
                result[i++] = ' ';
                while (i < text.Length)
                {
                    if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '/')
                    {
                        result[i++] = ' ';
                        result[i++] = ' ';
                        break;
                    }
                    result[i++] = ' ';
                }
            }
            else
            {
                i++;
            }
        }
        return new string(result);
    }

    private static void UnwrapRenderExtension(ref ExpressionSyntax baseExpr, ref string? explicitExtensionMode)
    {
        if (baseExpr is InvocationExpressionSyntax invExpr &&
            invExpr.Expression is MemberAccessExpressionSyntax invMa)
        {
            var methodName = invMa.Name.Identifier.Text;
            
            if (methodName == "AsDeclaration")
            {
                baseExpr = invMa.Expression;
                explicitExtensionMode = "decl";
            }
            else if (methodName == "AsAlias")
            {
                baseExpr = invMa.Expression;
                explicitExtensionMode = "alias";
            }
            else if (methodName == "AsBase")
            {
                baseExpr = invMa.Expression;
                explicitExtensionMode = "base";
            }
            else if (methodName == "AsColumn")
            {
                baseExpr = invMa.Expression;
                explicitExtensionMode = "col";
            }
        }
    }

    private static Dictionary<string, (string Open, string Close)> ExtractDialectQuotes(
        ImmutableArray<string> dialects, 
        Compilation compilation)
    {
        var quoteMap = new Dictionary<string, (string Open, string Close)>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var dialectName in dialects)
        {
            string openQuote = "\""; 
            string closeQuote = "\"";

            var dialectType = compilation.GetTypeByMetadataName(dialectName)
                           ?? compilation.GetTypeByMetadataName($"SqlInterpol.Dialects.{dialectName}Dialect")
                           ?? compilation.GetTypeByMetadataName($"SqlInterpol.{dialectName}Dialect");

            if (dialectType != null)
            {
                var quoteAttribute = dialectType.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name == "SqlDialectAttribute" || a.AttributeClass?.Name == "SqlDialect");

                if (quoteAttribute != null)
                {
                    var openArg = quoteAttribute.NamedArguments.FirstOrDefault(n => n.Key == "OpenQuote");
                    if (openArg.Value.Value is string o) openQuote = o;

                    var closeArg = quoteAttribute.NamedArguments.FirstOrDefault(n => n.Key == "CloseQuote");
                    if (closeArg.Value.Value is string c) closeQuote = c;
                }
            }
            
            quoteMap[dialectName] = (openQuote, closeQuote);
        }
        
        return quoteMap;
    }
}