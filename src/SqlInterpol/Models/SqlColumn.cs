using SqlInterpol.Constants;

namespace SqlInterpol.Models;

public class SqlColumn(SqlTable table, string name, string? alias = null) : SqlReference(name, alias)
{
    public SqlTable Table { get; } = table;

    public override string FullName => $"{Table.Reference}.[{Name}]";

    public override string Reference => Alias() != null ? $"[{Alias()}]" : FullName;

    public override string ToString()
    {
        // Default (no clause context) - always show full qualified name, never aliases
        // Aliases should only be rendered when explicitly in SELECT or ORDER BY context
        return ToString("DEFAULT", Sql.CurrentOptions);
    }

    public override string ToString(string clause)
    {
        return ToString(clause, Sql.CurrentOptions);
    }

    public override string ToString(string clause, SqlQueryOptions options)
    {
        return ToString(clause, options, isInAsContext: false);
    }

    public override string ToString(string clause, SqlQueryOptions options, bool isInAsContext)
    {
        try
        {
            IsAsAlias = isInAsContext;  // Set for this occurrence
            
            var start = options.IdentifierStart;
            var end = options.IdentifierEnd;
            
            // Build full qualified column name with proper identifiers
            var tableAlias = Table.Alias();
            var tableRef = tableAlias != null 
                ? $"{start}{tableAlias}{end}"
                : $"{(Table.Schema() != null
                    ? $"{start}{Table.Schema()}{end}."
                    : null)}{start}{Table.Name}{end}";
            var columnName = $"{start}{Name}{end}";
            var fullColumnName = $"{tableRef}.{columnName}";

            // AS_ALIAS: render just the alias name (used in "column AS alias" syntax)
            if (clause == SqlKeyword.AsAlias)
            {
                return $"{start}{Alias()}{end}";
            }

            // If this column is marked as being in an AS alias context and we're in SELECT,
            // render only the full name (the " AS alias" part comes from the template and Alias() return value)
            if (IsAsAlias && clause == SqlKeyword.Select)
            {
                return fullColumnName;
            }

            // Default/ON/JOIN: always show full qualified name without SELECT aliases
            if (clause == SqlKeyword.Default || clause == SqlKeyword.On || clause == SqlKeyword.JoinOn)
            {
                return fullColumnName;
            }

            if (Alias() != null)
            {
                // In SELECT, show "columnRef AS [alias]"
                // But NOT if IsAsAlias - that's handled above
                if (clause == SqlKeyword.Select && !IsAsAlias)
                {
                    return $"{fullColumnName} AS {start}{Alias()}{end}";
                }
                // In other clauses (ORDER BY, etc), show just "[alias]"
                else if (clause != SqlKeyword.Select)
                {
                    return $"{start}{Alias()}{end}";
                }
            }

            // In INSERT column list, show just the column name without table reference
            if (clause == SqlKeyword.Insert)
            {
                return $"{start}{Name}{end}";
            }

            // In UPDATE SET clause, show just the column name
            if (clause == SqlKeyword.Set)
            {
                return $"{start}{Name}{end}";
            }

            return fullColumnName;
        }
        finally
        {
            IsAsAlias = false;  // Always reset after rendering
        }
    }

    public override SqlReference As(string alias)
    {
        _alias = alias;
        
        return this;
    }
}