using System;
using System.Collections;
using System.Text;
using SqlInterpol.Parsing;

namespace SqlInterpol.Test.Parsing;

public class SqlInListFragment(IEnumerable items) : ISqlFragment
{
    private readonly IEnumerable _items = items;

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        var sb = new StringBuilder("(");
        bool first = true;

        foreach (var item in _items)
        {
            if (!first) sb.Append(", ");
            first = false;

            // The new AddParameter method securely handles all index tracking, 
            // prefix generation, and dictionary assignment internally!
            string paramKey = context.AddParameter(item);

            sb.Append(paramKey);
        }

        if (first) sb.Append("NULL"); // Safe fallback for empty lists
        sb.Append(")");

        return sb.ToString();
    }
}