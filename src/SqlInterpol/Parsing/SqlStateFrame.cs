namespace SqlInterpol.Parsing;

/// <summary>
/// A lightweight structural snapshot of clause metadata at a specific parenthesis layer.
/// </summary>
public struct SqlStateFrame
{
    /// <summary>
    /// The most recent primary SQL keyword encountered in the current frame (e.g., "SELECT", "FROM", "WHERE").
    /// </summary>
    public string? Keyword;

    /// <summary>
    /// The semantic tag associated with the current clause (e.g., <see cref="SqlSegmentTag.SelectKeyword"/>), 
    /// used to identify which part of the AST is currently being processed.
    /// </summary>
    public string? ClauseTag;

    /// <summary>
    /// Tracks the active Data Manipulation Language (DML) keyword (e.g., "INSERT", "UPDATE", "DELETE") 
    /// governing the current frame, if any.
    /// </summary>
    public string? ActiveDmlKeyword;

    /// <summary>
    /// The number of FROM clauses encountered within this specific parenthesis depth layer.
    /// Used to distinguish between primary targets and subsequent joins.
    /// </summary>
    public int FromCount;

    /// <summary>
    /// A flag indicating whether entity segments in the current context must force 
    /// their base table names to be rendered (e.g., during an INSERT or the target of an UPDATE) 
    /// instead of using their aliases.
    /// </summary>
    public bool ForceBaseNamePhase;
}