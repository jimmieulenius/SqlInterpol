using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlInterpol.Parsing;

public partial class SqlSegmentPreprocessor
{
    /// <summary>
    /// Evaluates DML structural contexts (like INSERT column lists) and delegates to standard target tracking.
    /// </summary>
    private bool ProcessDmlContext(ref SqlSegment segment, IReadOnlyList<SqlSegment> originalSegments, SqlPreprocessorState state)
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

    private bool ProcessTargetTracking(SqlSegment segment, IReadOnlyList<SqlSegment> segments, SqlPreprocessorState state)
    {
        if (segment.Value is SqlDynamicColumnFragment dynCol)
        {
            var colRef = ResolveDynamicColumn(dynCol, segments, state.Context);
            var mode = segment.RenderMode ?? (state.ForceBaseNamePhase ? SqlRenderMode.BaseName : null);
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

        // =========================================================================
        // DTO MACRO EXPANSION ENGINE
        // =========================================================================
        if (segment.Value is ISqlExpandable expandable)
        {
            if (state.ActiveEntityTarget == null)
                throw new InvalidOperationException($"Cannot expand DTO '{expandable.DtoType.Name}': No active entity target found in the current statement (e.g., missing INSERT INTO {{entity}}).");

            var meta = SqlMetadataRegistry.GetMetadata(state.ActiveEntityTarget.ModelType);
            var props = SqlMetadataRegistry.GetDtoProperties(expandable.DtoType);
            var dialect = state.Context.Dialect;

            var cols = new List<string>();
            var argNames = new List<string>();

            // Detect Context (INSERT vs UPDATE)
            bool isUpdateContext = state.ActiveDmlKeyword == SqlKeyword.Update.Value || state.CurrentClauseTag == SqlSegmentTag.SetKeyword;

            foreach (var prop in props)
            {
                // Optionally skip Primary Keys if we are expanding an UPDATE SET clause
                if (isUpdateContext && expandable.KeyProperties.Contains(prop.Name)) continue;

                var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                if (matchingKey != null)
                {
                    cols.Add(dialect.QuoteIdentifier(meta.Columns[matchingKey]));
                    argNames.Add(prop.Name);
                }
            }

            if (cols.Count == 0) 
                throw new InvalidOperationException($"No matching columns found between {state.ActiveEntityTarget.ModelType.Name} and {expandable.DtoType.Name}.");

            if (isUpdateContext)
            {
                // Expands into: Col1 = @p1, Col2 = @p2
                for (int c = 0; c < cols.Count; c++)
                {
                    if (c > 0) state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, ", "));
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, $"{cols[c]} = "));
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlArgumentFragment(argNames[c])));
                }
            }
            else
            {
                // Look-back to detect if the user explicitly typed the VALUES keyword
                int valuesIndex = -1;
                bool hasExplicitColumns = false;

                for (int i = state.Refined.Count - 1; i >= 0; i--)
                {
                    if (state.Refined[i].HasTag(SqlSegmentTag.InsertValuesKeyword))
                    {
                        valuesIndex = i;
                        
                        // Check if they also explicitly provided a column list like: (Id) VALUES
                        for (int j = i - 1; j >= 0; j--)
                        {
                            if (state.Refined[j].Type == SqlSegmentType.Literal)
                            {
                                var text = (string)state.Refined[j].Value!;
                                if (string.IsNullOrWhiteSpace(text)) continue;
                                if (text.TrimEnd().EndsWith(")")) hasExplicitColumns = true;
                                break;
                            }
                            if (state.Refined[j].Type != SqlSegmentType.Literal) break;
                        }
                        break;
                    }
                    
                    if (state.Refined[i].Type != SqlSegmentType.Literal || !string.IsNullOrWhiteSpace((string)state.Refined[i].Value!))
                    {
                        break;
                    }
                }

                if (valuesIndex >= 0)
                {
                    if (!hasExplicitColumns)
                    {
                        // AST Hijack: Safely drop the column list right BEFORE the VALUES keyword!
                        state.Refined.Insert(valuesIndex, new SqlSegment(SqlSegmentType.Literal, $"({string.Join(", ", cols)}){Environment.NewLine}"));
                    }
                    
                    // At the current position (AFTER the VALUES keyword), drop the parameter holes
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, "("));
                    for (int c = 0; c < argNames.Count; c++)
                    {
                        if (c > 0) state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, ", "));
                        state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlArgumentFragment(argNames[c])));
                    }
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, ")"));
                }
                else
                {
                    // Fallback: The user didn't write VALUES, so generate both automatically
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, $"({string.Join(", ", cols)}){Environment.NewLine}{SqlKeyword.Values.Value} ("));
                    for (int c = 0; c < argNames.Count; c++)
                    {
                        if (c > 0) state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, ", "));
                        state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlArgumentFragment(argNames[c])));
                    }
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, ")"));
                }
            }

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
            var mode = segment.RenderMode ?? (state.ForceBaseNamePhase ? SqlRenderMode.BaseName : null);
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

                if (ContainsKeyword(text, SqlKeyword.Values.Value) || 
                    ContainsKeyword(text, SqlKeyword.Select.Value) || 
                    ContainsKeyword(text, SqlKeyword.Set.Value) || 
                    ContainsKeyword(text, SqlKeyword.Update.Value) || 
                    ContainsKeyword(text, SqlKeyword.From.Value))
                {
                    return false;
                }

                if (ContainsKeyword(text, SqlKeyword.Insert.Value))
                {
                    return true;
                }
            }
        }
        return false;
    }
}