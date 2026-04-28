namespace SqlInterpol.Config;

public interface ISqlDialect
{    
    SqlDialectKind Kind { get; }
    string OpenQuote { get; }
    string CloseQuote { get; }
    string ParameterPrefix { get; }

    string QuoteIdentifier(string name);

    string QuoteTableName(string table, string? schema = null);
    
    string GetParameterName(int index);

    string ApplyAlias(string source, string? alias = null);
}