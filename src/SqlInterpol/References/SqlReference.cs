namespace SqlInterpol;

/// <summary>
/// Abstract base for all SQL reference implementations, providing source fragment, alias,
/// alias-quoting flag, and fallback alias state.
/// </summary>
/// <param name="parent">The underlying SQL fragment this reference points to.</param>
public abstract class SqlReference(ISqlFragment parent) : ISqlReference, ISqlAliasable
{
    /// <summary>Gets the underlying SQL fragment this reference wraps (e.g. an entity or subquery).</summary>
    public ISqlFragment Source { get; } = parent;

    /// <summary>Gets or sets the alias to use when this reference appears in a SELECT projection.</summary>
    public string? Alias { get; set; }

    /// <summary>Gets or sets whether the alias should be wrapped in dialect-specific identifier quotes.</summary>
    public bool IsAliasQuoted { get; set; }

    /// <summary>Gets the alias to use when <see cref="Alias"/> is <see langword="null"/>.</summary>
    public string FallbackAlias { get; init; } = string.Empty;

    /// <inheritdoc />
    public abstract string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default);
}