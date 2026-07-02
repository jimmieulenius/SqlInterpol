namespace SqlInterpol.Parsing;

/// <summary>
/// A lightweight structural snapshot of clause metadata at a specific parenthesis layer.
/// </summary>
internal struct StateFrame
{
    public string? Keyword;
    public string? ClauseTag;
    public string? ActiveDmlKeyword;
    public int FromCount;
    public bool ForceBaseNamePhase;
}

/// <summary>
/// Encapsulates the tracking state during a single pass of the SqlSegmentPreprocessor.
/// </summary>
internal class PreprocessorState
{
    public readonly ISqlContext Context;
    public readonly List<SqlSegment> Refined;
    
    public bool InString;
    public bool InLineCmt;
    public bool InBlockCmt;
    
    public int ParenDepth;
    public int FromCount;
    public string? CurrentKeyword;
    public string? CurrentClauseTag;
    public string? ActiveDmlKeyword;
    public bool ExpectsAlias;
    public bool ForceBaseNamePhase;
    
    public ISqlEntityBase? ActiveEntityTarget;
    public object? LastAliasableTarget;
    public ISqlReference? LastEntityRef;

    private readonly List<StateFrame> _stateStack = new(8);

    public PreprocessorState(ISqlContext context, int capacity)
    {
        Context = context;
        Refined = new List<SqlSegment>(capacity);
    }

    public void PushState()
    {
        ParenDepth++; 
        _stateStack.Add(new StateFrame
        {
            Keyword = CurrentKeyword,
            ClauseTag = CurrentClauseTag,
            ActiveDmlKeyword = ActiveDmlKeyword,
            FromCount = FromCount,
            ForceBaseNamePhase = ForceBaseNamePhase
        });
    }

    public void PopState()
    {
        if (ParenDepth > 0)
        {
            ParenDepth--; 
            var top = _stateStack[^1];
            _stateStack.RemoveAt(_stateStack.Count - 1);

            CurrentKeyword = top.Keyword;
            CurrentClauseTag = top.ClauseTag;
            ActiveDmlKeyword = top.ActiveDmlKeyword;
            FromCount = top.FromCount;
            ForceBaseNamePhase = top.ForceBaseNamePhase;
        }
    }
}