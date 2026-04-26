namespace SqlInterpol.Models;

public abstract class SqlReference(string name, string? alias = null) : Sql(string.Empty)
{
    public string Name { get; set; } = name;

    protected string? _alias = alias;
    
    public bool IsAsAlias { get; set; } = false;

    public abstract string FullName { get; }

    public abstract string Reference { get; }

    public override string ToString() => Reference;

    public virtual string ToString(string clause) => ToString(clause, Sql.CurrentOptions);

    public virtual string ToString(string clause, SqlQueryOptions options) => ToString();

    public virtual string ToString(string clause, SqlQueryOptions options, bool isInAsContext) => ToString(clause, options);

    public string? Alias() => _alias;

    public object Alias(string alias)
    {
        var start = Sql.CurrentOptions.IdentifierStart;
        var end = Sql.CurrentOptions.IdentifierEnd;
        _alias = alias;
        
        // Return just the formatted alias for inline syntax
        // Example: {col} AS {col.Alias("name")} renders as [col] AS [name]
        // Example: {table} AS {table.Alias("t")} renders as [schema].[table] AS [t]
        return $"{start}{alias}{end}";
    }

    public abstract SqlReference As(string alias);
}