namespace SqlInterpol.Generators;

/// <summary>
/// The output of a single-pass pre-analysis over an interpolated SQL string.
/// Determines whether the string can be safely AOT-intercepted or must fall back
/// to the JIT runtime engine.
/// </summary>
internal sealed class SqlAotAnalysisResult
{
    /// <summary>
    /// Gets inline alias overrides keyed by entity variable name (e.g., <c>"p" → "prod"</c>).
    /// Populated when a <c>{p} AS prod</c> alias pattern is detected.
    /// </summary>
    public Dictionary<string, string> InlineAliases { get; } = new();

    /// <summary>
    /// Gets inline aliases for property holes, keyed by interpolation hole index.
    /// </summary>
    public Dictionary<int, string> InlinePropertyAliases { get; } = new();

    /// <summary>
    /// Gets replacement text for the segment following an alias hole, keyed by hole index.
    /// The alias expression is stripped from the replacement value.
    /// </summary>
    public Dictionary<int, string> ReplacementForNextText { get; } = new();

    /// <summary>Gets or sets whether the string contains an AS keyword or an inline alias pattern.</summary>
    public bool HasAsKeywordOrAlias { get; set; }

    /// <summary>Gets or sets whether the string contains non-entity interpolation holes (raw parameter values).</summary>
    public bool HasParameterHoles { get; set; }

    /// <summary>Gets or sets whether a RETURNING clause is present (requires JIT for dialect rewriting).</summary>
    public bool HasReturning { get; set; }

    /// <summary>Gets or sets whether the string contains an INSERT, UPDATE, or DELETE keyword.</summary>
    public bool IsDmlQuery { get; set; }

    /// <summary>Gets or sets whether the string has complex dynamic holes (method calls, member access in ORDER BY / GROUP BY).</summary>
    public bool HasComplexDynamicHoles { get; set; }

    /// <summary>Gets or sets whether the string contains a set operation (UNION, EXCEPT, INTERSECT).</summary>
    public bool HasSetOperation { get; set; }

    /// <summary>Gets or sets whether an alias follows a closing bracket that the emitter cannot safely consume.</summary>
    public bool HasUnconsumableAlias { get; set; }

    /// <summary>Gets or sets whether an OVER(...) window function clause is present.</summary>
    public bool HasWindowFunction { get; set; }

    /// <summary>Gets or sets whether an ON CONFLICT / ON DUPLICATE KEY upsert pattern is present.</summary>
    public bool HasUpsert { get; set; }

    /// <summary>Gets or sets whether an interpolation hole appears directly after the AS keyword (alias-hole pattern).</summary>
    public bool HasHoleAfterAs { get; set; }

    /// <summary>
    /// Gets or sets the last SQL clause keyword detected during the pre-pass scan
    /// (e.g., <c>"WHERE"</c>, <c>"ORDER BY"</c>). Defaults to <c>"UNKNOWN"</c>.
    /// </summary>
    public string PrePassClause { get; set; } = "UNKNOWN";

    /// <summary>
    /// Gets a value indicating whether this call-site must fall back to the JIT runtime engine.
    /// A fallback is triggered by any construct the AOT emitter cannot safely unroll at compile time.
    /// </summary>
    public bool RequiresJitFallback =>
        (HasAsKeywordOrAlias && HasParameterHoles)
        || HasHoleAfterAs
        || HasReturning
        || HasComplexDynamicHoles
        || HasSetOperation
        || HasUnconsumableAlias
        || HasWindowFunction
        || HasUpsert;
}
