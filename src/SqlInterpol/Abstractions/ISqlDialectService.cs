namespace SqlInterpol.Abstractions;

public interface ISqlDialectService
{    
    string QuoteIdentifier(string name);

    string QuoteTableName(string table, string? schema = null);
    
    string GetParameterName(int index);

    string ApplyAlias(string source, string? alias = null);
}