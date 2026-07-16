using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SqlInterpol.Generators;

public partial class SqlAotInterceptorGenerator
{
    private static string Escape(string s) => s.Replace("\"", "\\\"");

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