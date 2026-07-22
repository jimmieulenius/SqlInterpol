using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

/// <summary>
/// Encapsulates the tracking state during a single pass of the SqlSegmentPreprocessor.
/// Extension rules can inspect this state to understand the context of the query,
/// or modify it to push custom segments and alter scope tracking.
/// </summary>
/// <param name="context">The active SQL compilation context, including the dialect and global options.</param>
/// <param name="capacity">The initial capacity for the refined segments list.</param>
public class SqlPreprocessorState(ISqlContext context, int capacity)
{
    /// <summary>The active SQL compilation context, including the dialect and global options.</summary>
    public readonly ISqlContext Context = context;

    /// <summary>The output list of refined segments currently being built by the preprocessor.</summary>
    public readonly List<SqlSegment> Refined = new(capacity);
    
    /// <summary>Indicates whether the lexer is currently inside a standard string literal (e.g., 'text').</summary>
    public bool InString;
    
    /// <summary>Indicates whether the lexer is currently inside a single-line comment (e.g., -- comment).</summary>
    public bool InLineCmt;
    
    /// <summary>Indicates whether the lexer is currently inside a multi-line block comment (e.g., /* comment */).</summary>
    public bool InBlockCmt;
    
    /// <summary>The current parenthesis nesting depth level.</summary>
    public int ParenDepth;
    
    /// <summary>The number of FROM clauses encountered in the current scope.</summary>
    public int FromCount;
    
    /// <summary>The most recent primary SQL keyword encountered (e.g., "SELECT", "WHERE").</summary>
    public string? CurrentKeyword;
    
    /// <summary>The semantic tag of the current clause (e.g., <see cref="SqlSegmentTag.SelectKeyword"/>).</summary>
    public string? CurrentClauseTag;
    
    /// <summary>The active Data Manipulation Language keyword governing the current statement (e.g., "INSERT", "UPDATE").</summary>
    public string? ActiveDmlKeyword;
    
    /// <summary>A flag indicating whether the lexer expects the next token to be an alias.</summary>
    public bool ExpectsAlias;
    
    /// <summary>A flag indicating whether entity aliases should be dropped in favor of rendering their base table names.</summary>
    public bool ForceBaseNamePhase;
    
    /// <summary>The primary target entity being operated on during an INSERT, UPDATE, or DELETE phase.</summary>
    public ISqlEntityBase? ActiveEntityTarget;
    
    /// <summary>The last processed item that is legally allowed to receive an AS alias.</summary>
    public object? LastAliasableTarget;
    
    /// <summary>The last entity reference encountered by the lexer.</summary>
    public ISqlReference? LastEntityRef;

    /// <summary>
    /// The stack of saved state frames representing outer parenthesis scopes.
    /// Extension rules can peek at this list to analyze outer context (e.g., <c>Frames[^1]</c> is the immediate parent scope).
    /// </summary>
    public readonly List<SqlStateFrame> Frames = new(8);

    /// <summary>
    /// Pushes the current contextual tracking variables into a new frame on the stack and increments parenthesis depth.
    /// </summary>
    public void PushState()
    {
        ParenDepth++; 
        Frames.Add(new SqlStateFrame
        {
            Keyword = CurrentKeyword,
            ClauseTag = CurrentClauseTag,
            ActiveDmlKeyword = ActiveDmlKeyword,
            FromCount = FromCount,
            ForceBaseNamePhase = ForceBaseNamePhase
        });
    }

    /// <summary>
    /// Pops the top state frame off the stack, restoring the contextual tracking variables of the parent scope.
    /// </summary>
    public void PopState()
    {
        if (ParenDepth > 0)
        {
            ParenDepth--; 
            var top = Frames[^1];
            Frames.RemoveAt(Frames.Count - 1);

            CurrentKeyword = top.Keyword;
            CurrentClauseTag = top.ClauseTag;
            ActiveDmlKeyword = top.ActiveDmlKeyword;
            FromCount = top.FromCount;
            ForceBaseNamePhase = top.ForceBaseNamePhase;
        }
    }
}