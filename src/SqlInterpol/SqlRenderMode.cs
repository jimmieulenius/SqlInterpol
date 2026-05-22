namespace SqlInterpol;

/// <summary>
/// Controls how a table, subquery, or column reference is rendered into the SQL output.
/// Passed to <see cref="ISqlFragment.ToSql"/> to select the appropriate representation
/// for the current position in a query (e.g. column list vs. FROM clause).
/// </summary>
public enum SqlRenderMode
{
    /// <summary>Renders the fully-qualified reference appropriate for the current context.</summary>
    Default,
    /// <summary>Renders only the alias or unqualified name (e.g. <c>stats</c>).</summary>
    AliasOnly,
    /// <summary>Renders the alias preceded by <c>AS</c> (e.g. <c>AS stats</c>).</summary>
    AsAlias,
    /// <summary>Renders the full declaration form used in a <c>FROM</c> or <c>JOIN</c> clause, including alias assignment.</summary>
    Declaration,
    /// <summary>Renders the bare base name without schema or alias (used in DDL and some dialect rewrites).</summary>
    BaseName
}