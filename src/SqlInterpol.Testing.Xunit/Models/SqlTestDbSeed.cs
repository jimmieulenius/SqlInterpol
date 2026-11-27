namespace SqlInterpol.Testing.Xunit;

/// <summary>
/// Represents a database seed definition for an E2E test case.
/// </summary>
public sealed record SqlTestDbSeed
{
    /// <summary>
    /// Gets the relative path or inline SQL string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the kind of seed represented by <see cref="Value"/>.
    /// </summary>
    public SqlSeedKind Kind { get; }

    private SqlTestDbSeed(string value, SqlSeedKind kind)
    {
        Value = value;
        Kind = kind;
    }

    /// <summary>
    /// Creates a seed targeting a relative file or folder inside the suite's <c>Db/Scripts</c> directory.
    /// </summary>
    /// <param name="relativePath">The folder name or <c>.sql</c> file name.</param>
    /// <returns>A new <see cref="SqlTestDbSeed"/> instance.</returns>
    public static SqlTestDbSeed Path(string relativePath) => new(relativePath, SqlSeedKind.Path);

    /// <summary>
    /// Creates a seed from an inline SQL statement.
    /// </summary>
    /// <param name="sql">The SQL script to execute.</param>
    /// <returns>A new <see cref="SqlTestDbSeed"/> instance.</returns>
    public static SqlTestDbSeed Inline(string sql) => new(sql, SqlSeedKind.Inline);
}