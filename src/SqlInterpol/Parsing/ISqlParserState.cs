namespace SqlInterpol.Parsing;

/// <summary>
/// Tracks mutable parser state across interpolated string segments within a single SQL query build.
/// </summary>
public interface ISqlParserState
{
    /// <summary>Gets or sets the most recently encountered SQL keyword that controls entity context.</summary>
    SqlKeyword? CurrentKeyword { get; set; }
    /// <summary>Gets or sets a value indicating whether the parser is currently inside a string literal.</summary>
    bool IsInsideString { get; set; }
    /// <summary>Gets or sets the number of parameters added to this query so far.</summary>
    int ParameterCount { get; set; }
    /// <summary>Gets or sets the last entity or projection that may receive an alias from the following token.</summary>
    ISqlFragment? LastAliasableTarget { get; set; }
    /// <summary>Gets or sets a value indicating whether the next interpolated value should be treated as an alias.</summary>
    bool ExpectsAliasOnly { get; set; }
    /// <summary>Gets or sets the most recently emitted segment.</summary>
    SqlSegment? LastSegment { get; set; }
    /// <summary>Gets the mapping from entities to their roles (table or CTE) within this query.</summary>
    Dictionary<ISqlEntityBase, SqlEntityRole> EntityRoles { get; }
    /// <summary>Gets or sets the entity currently receiving DML assignments (INSERT/UPDATE target).</summary>
    ISqlEntityBase? ActiveEntityTarget { get; set; }
    /// <summary>Gets or sets a value indicating whether the parser is currently inside a block comment.</summary>
    bool InBlockComment { get; set; }
    /// <summary>Gets or sets a value indicating whether the parser is currently inside a line comment.</summary>
    bool InLineComment { get; set; }
    /// <summary>Gets or sets the current parenthesis nesting depth.</summary>
    int ParenDepth { get; set; }
    
    /// <summary>Gets or sets the zero-based index of the segment currently being evaluated or rendered.</summary>
    int CurrentSegmentIndex { get; set; }

    /// <summary>Resets all parser state to its initial values for reuse.</summary>
    void Reset();
}