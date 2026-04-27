using System.Text.RegularExpressions;
using SqlInterpol.Abstractions;
using SqlInterpol.Constants;

namespace SqlInterpol.Services;

public abstract class SqlDialectServiceBase : ISqlDialectService
{
    // Specific dialects define these symbols
    public abstract string OpenQuote { get; }
    public abstract string CloseQuote { get; }
    public abstract string ParameterPrefix { get; }

    // Common logic for all dialects
    public virtual string QuoteIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var trimmed = name.Trim();
        // Use regex to check if already quoted
        var open = Regex.Escape(OpenQuote);
        var close = Regex.Escape(CloseQuote);
        var pattern = $@"^\s*{open}.*{close}\s*$";

        if (Regex.IsMatch(trimmed, pattern))
        {
            return trimmed;
        }

        return $"{OpenQuote}{trimmed}{CloseQuote}";
    }

    public virtual string QuoteTableName(string table, string? schema = null)
    {
        var quotedTable = QuoteIdentifier(table);

        if (string.IsNullOrWhiteSpace(schema))
        {
            return quotedTable;
        }
        
        return $"{QuoteIdentifier(schema)}.{quotedTable}";
    }

    public virtual string GetParameterName(int index)
    {
        // Default logic: @p0, @p1, etc.
        return $"{ParameterPrefix}{index}";
    }

    public string ApplyAlias(string source, string? alias = null)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return source;
        }

        return $"{source} {SqlKeyword.As} {QuoteIdentifier(alias)}";
    }
}