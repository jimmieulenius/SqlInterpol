using System.Collections;
using System.Reflection;

namespace SqlInterpol.Parsing;

public partial class SqlSegmentPreprocessor
{
    private bool ProcessUnresolved(SqlSegment segment, IReadOnlyList<SqlSegment> segments, SqlPreprocessorState state)
    {
        if (segment.Type != SqlSegmentType.Unresolved) return false;

        var value = segment.Value;
        
        // 1. FIX: Safely unwrap ISqlDeclaration to prevent it from triggering the DTO class check!
        ISqlEntityBase? entity = value as ISqlEntityBase;
        if (value is ISqlDeclaration declaration)
        {
            entity = declaration.Entity;
        }

        if (entity != null)
        {
            if (string.Equals(state.CurrentKeyword, SqlKeyword.Select.Value, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(state.CurrentKeyword, SqlKeyword.SelectDistinct.Value, StringComparison.OrdinalIgnoreCase))
            {
                var entityMeta = SqlMetadataRegistry.GetMetadata(entity.ModelType);
                var columns = new List<ISqlFragment>(entityMeta.SortedColumns.Count);
                
                foreach (var col in entityMeta.SortedColumns)
                {
                    var memberType = col.Key is PropertyInfo prop ? prop.PropertyType : ((FieldInfo)col.Key).FieldType;
                    if (memberType.IsClass && memberType != typeof(string) && memberType != typeof(byte[])) continue;
                    columns.Add(new SqlColumnReference(entity.Reference, col.Value, col.Key.Name));
                }

                state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlSelectFragment(columns, isDistinct: string.Equals(state.CurrentKeyword, SqlKeyword.SelectDistinct.Value, StringComparison.OrdinalIgnoreCase))));
                return true;
            }

            var mode = segment.RenderMode;
            if (mode == null && state.ForceBaseNamePhase) mode = SqlRenderMode.BaseName;
            state.Refined.Add(new SqlSegment(SqlSegmentType.Reference, entity, mode, segment.Tags));
            return true;
        }

        if (value is ISqlExpandable expandable)
        {
            if (state.ActiveEntityTarget == null) throw new InvalidOperationException("Sql.Expand requires an active entity context.");
            
            var entityMeta = SqlMetadataRegistry.GetMetadata(state.ActiveEntityTarget.ModelType);
            var dtoProps = SqlMetadataRegistry.GetDtoProperties(expandable.DtoType);
            var assignments = new List<ISqlAssignmentFragment>(dtoProps.Length);
            bool isSetClause = string.Equals(state.CurrentKeyword, SqlKeyword.Set.Value, StringComparison.OrdinalIgnoreCase);

            var sortedDtoProps = dtoProps.OrderBy(p => {
                var m = entityMeta.Columns.Keys.FirstOrDefault(k => k.Name == p.Name);
                return m != null ? entityMeta.Columns[m] : p.Name;
            }).ToArray();

            foreach (var prop in sortedDtoProps)
            {
                if (isSetClause && expandable.KeyProperties.Contains(prop.Name)) continue;
                var entityMember = entityMeta.Columns.Keys.FirstOrDefault(k => k.Name == prop.Name);
                if (entityMember != null)
                {
                    var colRef = new SqlColumnReference(state.ActiveEntityTarget.Reference, entityMeta.Columns[entityMember], prop.Name);
                    assignments.Add(new SqlAssignmentFragment(colRef, Sql.Arg(prop.Name)));
                }
            }

            if (isSetClause) state.Refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlSetFragment(assignments)));
            else if (string.Equals(state.CurrentKeyword, SqlKeyword.Select.Value, StringComparison.OrdinalIgnoreCase) || string.Equals(state.CurrentKeyword, SqlKeyword.SelectDistinct.Value, StringComparison.OrdinalIgnoreCase))
            {
                var columns = assignments.Select(a => (ISqlFragment)a.Reference).ToList();
                state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlSelectFragment(columns, isDistinct: string.Equals(state.CurrentKeyword, SqlKeyword.SelectDistinct.Value, StringComparison.OrdinalIgnoreCase))));
            }
            else state.Refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlInsertValuesFragment(assignments)));
            return true;
        }

        if (value != null && value.GetType().IsClass && value is not string && value is not ISqlFragment)
        {
            bool isIterable = value is IEnumerable && value is not byte[];

            // Determine context comprehensively using ActiveDmlKeyword for safety
            bool isInsertContext = string.Equals(state.ActiveDmlKeyword, SqlKeyword.Insert.Value, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(state.CurrentKeyword, SqlKeyword.Insert.Value, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(state.CurrentKeyword, SqlKeyword.Into.Value, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(state.CurrentKeyword, SqlKeyword.Values.Value, StringComparison.OrdinalIgnoreCase);

            bool isSetContext = string.Equals(state.CurrentKeyword, SqlKeyword.Set.Value, StringComparison.OrdinalIgnoreCase);

            if (!isIterable || isInsertContext)
            {
                if (isSetContext)
                {
                    if (state.ActiveEntityTarget == null) throw new InvalidOperationException($"DTO context missing for target mapping.");
                    var assignments = Sql.BuildAssignments(state.ActiveEntityTarget, value, state.Context);
                    foreach (var a in assignments) if (a is ISqlParameterGenerator gen) gen.GenerateParameters(state.Context);
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlSetFragment(assignments), segment.RenderMode, segment.Tags));
                    return true;
                }
                else if (isInsertContext)
                {
                    if (state.ActiveEntityTarget == null) throw new InvalidOperationException($"DTO context missing for target mapping.");
                    if (isIterable)
                    {
                        var bulkAssignments = new List<IReadOnlyList<ISqlAssignmentFragment>>();
                        foreach (var item in (IEnumerable)value)
                        {
                            var itemAssignments = Sql.BuildAssignments(state.ActiveEntityTarget, item, state.Context);
                            foreach (var a in itemAssignments) if (a is ISqlParameterGenerator gen) gen.GenerateParameters(state.Context);
                            bulkAssignments.Add(itemAssignments);
                        }
                        state.Refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlInsertValuesFragment(bulkAssignments), segment.RenderMode, segment.Tags));
                    }
                    else
                    {
                        var assignments = Sql.BuildAssignments(state.ActiveEntityTarget, value, state.Context);
                        foreach (var a in assignments) if (a is ISqlParameterGenerator gen) gen.GenerateParameters(state.Context);
                        state.Refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlInsertValuesFragment(assignments), segment.RenderMode, segment.Tags));
                    }
                    return true;
                }
            }
        }

        if (value is IEnumerable databaseIterable && value is not string && value is not byte[])
        {
            int estimatedCount = databaseIterable is ICollection collection ? collection.Count : 4;

            bool isFragmentCollection = false;
            foreach (var element in databaseIterable)
            {
                if (element is ISqlFragment) { isFragmentCollection = true; break; }
                if (element != null) break;
            }

            if (isFragmentCollection)
            {
                var fragments = new List<ISqlFragment>(estimatedCount);
                foreach (var element in databaseIterable)
                {
                    if (element is ISqlFragment frag)
                    {
                        if (frag is SqlDynamicOrderFragment innerDynOrder)
                        {
                            var colRef = ResolveDynamicColumn(innerDynOrder.Column, segments, state.Context);
                            fragments.Add(innerDynOrder.Direction.HasValue ? new SqlOrderFragment(colRef, innerDynOrder.Direction.Value) : new SqlOrderFragment(colRef));
                        }
                        else if (frag is SqlDynamicColumnFragment innerDynCol)
                        {
                            fragments.Add(ResolveDynamicColumn(innerDynCol, segments, state.Context));
                        }
                        else fragments.Add(frag);
                    }
                }
                
                state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlCollectionFragment(fragments), segment.RenderMode, segment.Tags));
                return true;
            }

            var parameterizedKeys = new List<string>(estimatedCount);
            foreach (var element in databaseIterable) parameterizedKeys.Add(state.Context.AddParameter(element));
            state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlRawCollectionFragment(parameterizedKeys), segment.RenderMode, segment.Tags));
            return true;
        }

        string registeredParamKey = state.Context.AddParameter(value);
        state.Refined.Add(new SqlSegment(SqlSegmentType.Parameter, registeredParamKey, segment.RenderMode, segment.Tags));
        return true;
    }
}