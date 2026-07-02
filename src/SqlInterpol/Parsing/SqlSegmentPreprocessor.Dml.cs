namespace SqlInterpol.Parsing;

public partial class SqlSegmentPreprocessor
{
    /// <summary>
    /// Evaluates DML structural contexts (like INSERT column lists) and delegates to standard target tracking.
    /// </summary>
    private bool ProcessDmlContext(ref SqlSegment segment, IReadOnlyList<SqlSegment> originalSegments, PreprocessorState state)
    {
        // 1. Structural Analysis: Auto-Detect if we are inside an INSERT INTO (...) column list
        if (segment.Value is SqlColumnReferenceBase && segment.RenderMode == null)
        {
            if (IsInInsertColumnList(state.Refined))
            {
                // Permanently bake the RenderMode into the AST node
                segment = new SqlSegment(segment.Type, segment.Value, SqlRenderMode.BaseName, segment.Tags);
            }
        }

        // 2. Delegate to standard target tracking
        return ProcessTargetTracking(segment, originalSegments, state);
    }

    private bool ProcessTargetTracking(SqlSegment segment, IReadOnlyList<SqlSegment> segments, PreprocessorState state)
    {
        if (segment.Value is SqlDynamicColumnFragment dynCol)
        {
            var colRef = ResolveDynamicColumn(dynCol, segments, state.Context);
            var mode = segment.RenderMode;
            if (mode == null && state.ForceBaseNamePhase) mode = SqlRenderMode.BaseName;
            
            state.Refined.Add(new SqlSegment(SqlSegmentType.Projection, colRef, mode, segment.Tags));
            return true;
        }

        if (segment.Value is SqlDynamicOrderFragment dynOrder)
        {
            var colRef = ResolveDynamicColumn(dynOrder.Column, segments, state.Context);
            var resolvedFragment = dynOrder.Direction.HasValue ? new SqlOrderFragment(colRef, dynOrder.Direction.Value) : new SqlOrderFragment(colRef);
            state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, resolvedFragment, segment.RenderMode, segment.Tags));
            return true;
        }

        if (segment.Value is ISqlEntityBase structuralEntity1)
        {
            state.ActiveEntityTarget = structuralEntity1;
            state.LastAliasableTarget = structuralEntity1;
            state.LastEntityRef = structuralEntity1.Reference;
        }
        else if (segment.Type == SqlSegmentType.Projection)
        {
            state.LastAliasableTarget = segment.Value;
            var mode = segment.RenderMode;
            if (mode == null && state.ForceBaseNamePhase) mode = SqlRenderMode.BaseName;
            
            state.Refined.Add(new SqlSegment(segment.Type, segment.Value, mode, segment.Tags));
            return true;
        }
        else if (segment.Value is ISqlDeclaration decl)
        {
            state.ActiveEntityTarget = decl.Entity;
            state.LastAliasableTarget = decl.Entity;
            state.LastEntityRef = decl.Entity.Reference;
        }

        return false;
    }

    private static bool IsInInsertColumnList(IReadOnlyList<SqlSegment> refinedSegments)
    {
        for (int i = refinedSegments.Count - 1; i >= 0; i--)
        {
            if (refinedSegments[i].Type == SqlSegmentType.Literal && refinedSegments[i].Value is string text)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Modernized to use standard SqlKeyword constants instead of magic string literals
                if (ContainsKeyword(text, SqlKeyword.Values.Value) || 
                    ContainsKeyword(text, SqlKeyword.Select.Value) || 
                    ContainsKeyword(text, SqlKeyword.Set.Value) || 
                    ContainsKeyword(text, SqlKeyword.Update.Value) || 
                    ContainsKeyword(text, SqlKeyword.From.Value))
                {
                    return false;
                }

                // Tracing backward to an INSERT statement definitively identifies an INSERT target column list context
                if (ContainsKeyword(text, SqlKeyword.Insert.Value))
                {
                    return true;
                }
            }
        }
        return false;
    }
}