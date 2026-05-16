using SqlInterpol.Config;
using SqlInterpol.Parsing;
using System.Collections;
using System.Text;

namespace SqlInterpol.Test.Parsing;

public class SqlInListFragment(IEnumerable items) : ISqlFragment
{
    private readonly IEnumerable _items = items;

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        var sb = new StringBuilder("(");
        bool first = true;
        var parserContext = (ISqlParserContext)context;

        foreach (var item in _items)
        {
            if (!first) sb.Append(", ");
            first = false;

            // Safely generate parameter keys based on context state
            int index = parserContext.Options.ParameterIndexStart + parserContext.ParserState.ParameterCount;
            string prefix = parserContext.Options.ParameterPrefixOverride ?? parserContext.Dialect.ParameterPrefix;
            string paramKey = $"{prefix}{index}";

            // Add to the builder's dictionary
            parserContext.Parameters[paramKey] = item ?? DBNull.Value;
            parserContext.ParserState.ParameterCount++;

            sb.Append(paramKey);
        }

        if (first) sb.Append("NULL"); // Safe fallback for empty lists
        sb.Append(")");

        return sb.ToString();
    }
}