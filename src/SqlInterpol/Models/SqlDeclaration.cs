using SqlInterpol.Abstractions;

namespace SqlInterpol.Models;

public class SqlDeclaration(ISqlReference reference) : ISqlDeclaration
{
    public ISqlReference Reference { get; } = reference;

    public string ToSql(SqlContext context)
    {
        // The Declaration asks the projection for its 'Source' name 
        // (e.g., "Product" or "(SELECT...)"), then applies the alias 
        // stored in the Reference.
        var sourceSql = Reference.Source.ToSql(context);
        
        return context.Dialect.ApplyAlias(sourceSql, Reference.Alias);
    }
}