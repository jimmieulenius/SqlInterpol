namespace SqlInterpol;

/// <summary>
/// Represents a typed SQL reference — a column or expression — that can carry a rendering alias.
/// </summary>
/// <remarks>
/// References appear in SELECT projections, ORDER BY clauses, and WHERE conditions.
/// Setting <see cref="Alias"/> causes the reference to render with <c>AS "alias"</c> in declaration contexts.
/// </remarks>
public interface ISqlReference : ISqlFragment
{
    /// <summary>Gets the underlying SQL fragment this reference points to (e.g. a column or subquery).</summary>
    ISqlFragment Source { get; }

    /// <summary>Gets or sets the alias to use when this reference appears in a SELECT projection.</summary>
    string? Alias { get; }

    /// <summary>Gets or sets whether the alias should be wrapped in dialect-specific identifier quotes.</summary>
    bool IsAliasQuoted { get; set; }

    /// <summary>Gets the alias to use when <see cref="Alias"/> is <see langword="null"/>.</summary>
    string FallbackAlias { get; }
}