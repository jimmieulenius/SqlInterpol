namespace SqlInterpol.Config;

public readonly record struct SqlDialectKind(string Value)
{
    public static SqlDialectKind MySql { get; } = new("MySql");
    public static SqlDialectKind Oracle { get; } = new("Oracle");
    public static SqlDialectKind PostgreSql { get; } = new("PostgreSql");
    public static SqlDialectKind SqLite { get; } = new("SqLite");
    public static SqlDialectKind SqlServer { get; } = new("SqlServer");
    
    public override string ToString() => Value;

    public static implicit operator string(SqlDialectKind kind) => kind.Value;
}