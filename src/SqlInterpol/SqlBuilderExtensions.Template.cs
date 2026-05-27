using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Provides extension methods for appending pre-compiled SQL templates to a <see cref="SqlBuilder"/>.
/// </summary>
public static partial class SqlBuilderExtensions    
{
    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, replacing the template's dummy entity 
    /// with the provided runtime entity, and optionally resolving local template arguments.
    /// </summary>
    /// <typeparam name="T1">The model type for the first entity.</typeparam>
    /// <param name="builder">The current SQL builder instance.</param>
    /// <param name="template">The pre-compiled SQL template to append.</param>
    /// <param name="entity1">The runtime entity (e.g., table or view) that will replace the template's first placeholder.</param>
    /// <param name="arguments">An optional object providing values for template arguments. Arguments not resolved here will be deferred to <c>Build()</c>.</param>
    /// <returns>The same <see cref="SqlBuilder"/> instance for chaining.</returns>
    public static SqlBuilder Append<T1>(
        this SqlBuilder builder, 
        SqlTemplate<T1> template, 
        ISqlEntityBase<T1> entity1, 
        object? arguments = null)
    {
        var entityMap = new Dictionary<ISqlReference, ISqlEntityBase>
        {
            { template.Entity1.Reference, entity1 }
        };

        return ImportSegments(builder, template.Segments, entityMap, arguments);
    }

    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, replacing the template's dummy entity 
    /// with the provided runtime entity, optionally resolving local template arguments, and appends a new line.
    /// </summary>
    /// <inheritdoc cref="Append{T1}(SqlBuilder, SqlTemplate{T1}, ISqlEntityBase{T1}, object?)" path="/typeparam|/param|/returns"/>
    public static SqlBuilder AppendLine<T1>(
        this SqlBuilder builder, 
        SqlTemplate<T1> template, 
        ISqlEntityBase<T1> entity1, 
        object? arguments = null)
    {
        builder.Append(template, entity1, arguments);
        builder.AppendSegment(new SqlSegment(SqlSegmentType.Literal, Environment.NewLine));
        return builder;
    }

    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, replacing the template's dummy entities 
    /// with the provided runtime entities.
    /// </summary>
    /// <typeparam name="T1">The model type for the first entity.</typeparam>
    /// <typeparam name="T2">The model type for the second entity.</typeparam>
    /// <param name="builder">The current SQL builder instance.</param>
    /// <param name="template">The pre-compiled SQL template to append.</param>
    /// <param name="entity1">The runtime entity that will replace the template's first placeholder.</param>
    /// <param name="entity2">The runtime entity that will replace the template's second placeholder.</param>
    /// <param name="arguments">An optional object providing values for template arguments.</param>
    /// <returns>The same <see cref="SqlBuilder"/> instance for chaining.</returns>
    public static SqlBuilder Append<T1, T2>(
        this SqlBuilder builder, 
        SqlTemplate<T1, T2> template, 
        ISqlEntityBase<T1> entity1, 
        ISqlEntityBase<T2> entity2, 
        object? arguments = null)
    {
        var entityMap = new Dictionary<ISqlReference, ISqlEntityBase>
        {
            { template.Entity1.Reference, entity1 },
            { template.Entity2.Reference, entity2 }
        };

        return ImportSegments(builder, template.Segments, entityMap, arguments);
    }

    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, replacing the template's dummy entities 
    /// with the provided runtime entities, and appends a new line.
    /// </summary>
    /// <inheritdoc cref="Append{T1, T2}(SqlBuilder, SqlTemplate{T1, T2}, ISqlEntityBase{T1}, ISqlEntityBase{T2}, object?)" path="/typeparam|/param|/returns"/>
    public static SqlBuilder AppendLine<T1, T2>(
        this SqlBuilder builder, 
        SqlTemplate<T1, T2> template, 
        ISqlEntityBase<T1> entity1, 
        ISqlEntityBase<T2> entity2, 
        object? arguments = null)
    {
        builder.Append(template, entity1, entity2, arguments);
        builder.AppendSegment(new SqlSegment(SqlSegmentType.Literal, Environment.NewLine));
        return builder;
    }

    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, replacing the template's dummy entities 
    /// with the provided runtime entities.
    /// </summary>
    /// <typeparam name="T1">The model type for the first entity.</typeparam>
    /// <typeparam name="T2">The model type for the second entity.</typeparam>
    /// <typeparam name="T3">The model type for the third entity.</typeparam>
    /// <param name="builder">The current SQL builder instance.</param>
    /// <param name="template">The pre-compiled SQL template to append.</param>
    /// <param name="entity1">The runtime entity that will replace the template's first placeholder.</param>
    /// <param name="entity2">The runtime entity that will replace the template's second placeholder.</param>
    /// <param name="entity3">The runtime entity that will replace the template's third placeholder.</param>
    /// <param name="arguments">An optional object providing values for template arguments.</param>
    /// <returns>The same <see cref="SqlBuilder"/> instance for chaining.</returns>
    public static SqlBuilder Append<T1, T2, T3>(
        this SqlBuilder builder, 
        SqlTemplate<T1, T2, T3> template, 
        ISqlEntityBase<T1> entity1, 
        ISqlEntityBase<T2> entity2, 
        ISqlEntityBase<T3> entity3, 
        object? arguments = null)
    {
        var entityMap = new Dictionary<ISqlReference, ISqlEntityBase>
        {
            { template.Entity1.Reference, entity1 },
            { template.Entity2.Reference, entity2 },
            { template.Entity3.Reference, entity3 }
        };

        return ImportSegments(builder, template.Segments, entityMap, arguments);
    }

    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, replacing the template's dummy entities 
    /// with the provided runtime entities, and appends a new line.
    /// </summary>
    /// <inheritdoc cref="Append{T1, T2, T3}(SqlBuilder, SqlTemplate{T1, T2, T3}, ISqlEntityBase{T1}, ISqlEntityBase{T2}, ISqlEntityBase{T3}, object?)" path="/typeparam|/param|/returns"/>
    public static SqlBuilder AppendLine<T1, T2, T3>(
        this SqlBuilder builder, 
        SqlTemplate<T1, T2, T3> template, 
        ISqlEntityBase<T1> entity1, 
        ISqlEntityBase<T2> entity2, 
        ISqlEntityBase<T3> entity3, 
        object? arguments = null)
    {
        builder.Append(template, entity1, entity2, entity3, arguments);
        builder.AppendSegment(new SqlSegment(SqlSegmentType.Literal, Environment.NewLine));
        return builder;
    }

    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, replacing the template's dummy entities 
    /// with the provided runtime entities.
    /// </summary>
    /// <typeparam name="T1">The model type for the first entity.</typeparam>
    /// <typeparam name="T2">The model type for the second entity.</typeparam>
    /// <typeparam name="T3">The model type for the third entity.</typeparam>
    /// <typeparam name="T4">The model type for the fourth entity.</typeparam>
    /// <param name="builder">The current SQL builder instance.</param>
    /// <param name="template">The pre-compiled SQL template to append.</param>
    /// <param name="entity1">The runtime entity that will replace the template's first placeholder.</param>
    /// <param name="entity2">The runtime entity that will replace the template's second placeholder.</param>
    /// <param name="entity3">The runtime entity that will replace the template's third placeholder.</param>
    /// <param name="entity4">The runtime entity that will replace the template's fourth placeholder.</param>
    /// <param name="arguments">An optional object providing values for template arguments.</param>
    /// <returns>The same <see cref="SqlBuilder"/> instance for chaining.</returns>
    public static SqlBuilder Append<T1, T2, T3, T4>(
        this SqlBuilder builder, 
        SqlTemplate<T1, T2, T3, T4> template, 
        ISqlEntityBase<T1> entity1, 
        ISqlEntityBase<T2> entity2, 
        ISqlEntityBase<T3> entity3, 
        ISqlEntityBase<T4> entity4, 
        object? arguments = null)
    {
        var entityMap = new Dictionary<ISqlReference, ISqlEntityBase>
        {
            { template.Entity1.Reference, entity1 },
            { template.Entity2.Reference, entity2 },
            { template.Entity3.Reference, entity3 },
            { template.Entity4.Reference, entity4 }
        };

        return ImportSegments(builder, template.Segments, entityMap, arguments);
    }

    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, replacing the template's dummy entities 
    /// with the provided runtime entities, and appends a new line.
    /// </summary>
    /// <inheritdoc cref="Append{T1, T2, T3, T4}(SqlBuilder, SqlTemplate{T1, T2, T3, T4}, ISqlEntityBase{T1}, ISqlEntityBase{T2}, ISqlEntityBase{T3}, ISqlEntityBase{T4}, object?)" path="/typeparam|/param|/returns"/>
    public static SqlBuilder AppendLine<T1, T2, T3, T4>(
        this SqlBuilder builder, 
        SqlTemplate<T1, T2, T3, T4> template, 
        ISqlEntityBase<T1> entity1, 
        ISqlEntityBase<T2> entity2, 
        ISqlEntityBase<T3> entity3, 
        ISqlEntityBase<T4> entity4, 
        object? arguments = null)
    {
        builder.Append(template, entity1, entity2, entity3, entity4, arguments);
        builder.AppendSegment(new SqlSegment(SqlSegmentType.Literal, Environment.NewLine));
        return builder;
    }

    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, replacing the template's dummy entities 
    /// with the provided runtime entities.
    /// </summary>
    /// <typeparam name="T1">The model type for the first entity.</typeparam>
    /// <typeparam name="T2">The model type for the second entity.</typeparam>
    /// <typeparam name="T3">The model type for the third entity.</typeparam>
    /// <typeparam name="T4">The model type for the fourth entity.</typeparam>
    /// <typeparam name="T5">The model type for the fifth entity.</typeparam>
    /// <param name="builder">The current SQL builder instance.</param>
    /// <param name="template">The pre-compiled SQL template to append.</param>
    /// <param name="entity1">The runtime entity that will replace the template's first placeholder.</param>
    /// <param name="entity2">The runtime entity that will replace the template's second placeholder.</param>
    /// <param name="entity3">The runtime entity that will replace the template's third placeholder.</param>
    /// <param name="entity4">The runtime entity that will replace the template's fourth placeholder.</param>
    /// <param name="entity5">The runtime entity that will replace the template's fifth placeholder.</param>
    /// <param name="arguments">An optional object providing values for template arguments.</param>
    /// <returns>The same <see cref="SqlBuilder"/> instance for chaining.</returns>
    public static SqlBuilder Append<T1, T2, T3, T4, T5>(
        this SqlBuilder builder, 
        SqlTemplate<T1, T2, T3, T4, T5> template, 
        ISqlEntityBase<T1> entity1, 
        ISqlEntityBase<T2> entity2, 
        ISqlEntityBase<T3> entity3, 
        ISqlEntityBase<T4> entity4, 
        ISqlEntityBase<T5> entity5, 
        object? arguments = null)
    {
        var entityMap = new Dictionary<ISqlReference, ISqlEntityBase>
        {
            { template.Entity1.Reference, entity1 },
            { template.Entity2.Reference, entity2 },
            { template.Entity3.Reference, entity3 },
            { template.Entity4.Reference, entity4 },
            { template.Entity5.Reference, entity5 }
        };

        return ImportSegments(builder, template.Segments, entityMap, arguments);
    }

    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, replacing the template's dummy entities 
    /// with the provided runtime entities, and appends a new line.
    /// </summary>
    /// <inheritdoc cref="Append{T1, T2, T3, T4, T5}(SqlBuilder, SqlTemplate{T1, T2, T3, T4, T5}, ISqlEntityBase{T1}, ISqlEntityBase{T2}, ISqlEntityBase{T3}, ISqlEntityBase{T4}, ISqlEntityBase{T5}, object?)" path="/typeparam|/param|/returns"/>
    public static SqlBuilder AppendLine<T1, T2, T3, T4, T5>(
        this SqlBuilder builder, 
        SqlTemplate<T1, T2, T3, T4, T5> template, 
        ISqlEntityBase<T1> entity1, 
        ISqlEntityBase<T2> entity2, 
        ISqlEntityBase<T3> entity3, 
        ISqlEntityBase<T4> entity4, 
        ISqlEntityBase<T5> entity5, 
        object? arguments = null)
    {
        builder.Append(template, entity1, entity2, entity3, entity4, entity5, arguments);
        builder.AppendSegment(new SqlSegment(SqlSegmentType.Literal, Environment.NewLine));
        return builder;
    }

    /// <summary>
    /// Processes and imports the frozen segments of a template into the active builder, 
    /// resolving dummy entity placeholders, template structural branches, and tracking parameters.
    /// </summary>
    /// <param name="builder">The target SQL builder accumulating the output.</param>
    /// <param name="segments">The cached template segments to import.</param>
    /// <param name="entityMap">The dictionary mapping dummy references to real runtime entities.</param>
    /// <param name="arguments">The local argument payload object to read parameter values from.</param>
    /// <returns>The updated <see cref="SqlBuilder"/> instance.</returns>
    private static SqlBuilder ImportSegments(
        SqlBuilder builder, 
        IReadOnlyList<SqlSegment> segments, 
        Dictionary<ISqlReference, ISqlEntityBase> entityMap,
        object? arguments)
    {
        var argumentGetters = arguments != null 
            ? SqlMetadataRegistry.GetArgumentGetters(arguments.GetType()) 
            : null;

        foreach (var segment in segments)
        {
            // Update Contextual Keywords from the Cached Template to guide rendering
            if (segment.Tag == SqlSegmentTag.InsertKeyword) builder.Context.ParserState.CurrentKeyword = SqlKeyword.Insert;
            if (segment.Tag == SqlSegmentTag.UpdateKeyword) builder.Context.ParserState.CurrentKeyword = SqlKeyword.Update;
            if (segment.Tag == SqlSegmentTag.SetKeyword) builder.Context.ParserState.CurrentKeyword = SqlKeyword.Set;
            if (segment.Tag == SqlSegmentTag.SelectKeyword) builder.Context.ParserState.CurrentKeyword = SqlKeyword.Select;

            // 1. Branch Nodes
            if (segment.Value is ISqlSwappableFragment swappable)
            {
                var swappedFragment = swappable.Swap(entityMap, argumentGetters, arguments);
                
                // FORCE: Proactively notify the runtime parser state that these parameters are tracked!
                if (swappedFragment is ISqlParameterGenerator parameterGenerator)
                {
                    parameterGenerator.GenerateParameters(builder.Context);
                }
                
                builder.AppendSegment(new SqlSegment(segment.Type, swappedFragment, segment.RenderMode, segment.Tag));
                continue;
            }

            // 2. Leaf Node: Arguments
            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlArgumentFragment argFragment)
            {
                object? val = null;
                if (argumentGetters != null && argumentGetters.TryGetValue(argFragment.Name, out var getter))
                {
                    val = getter(arguments!);
                }
                
                string activeParamKey = builder.Context.AddParameter(val);
                builder.AppendSegment(new SqlSegment(SqlSegmentType.Parameter, activeParamKey, segment.RenderMode, segment.Tag));
                continue;
            }

            // 3. Leaf Node: Entities
            if (segment.Value is ISqlEntityBase dummyEntity && entityMap.TryGetValue(dummyEntity.Reference, out var realEntity))
            {
                builder.Context.ParserState.ActiveEntityTarget = realEntity;
                builder.AppendSegment(new SqlSegment(segment.Type, realEntity, segment.RenderMode, segment.Tag));
                continue;
            }

            // 4. Leaf Node: Columns (Supports both standard and raw column references)
            if (segment.Value is SqlColumnReferenceBase colRef && entityMap.TryGetValue(colRef.SourceReference, out var realSource))
            {
                var mappedColumn = colRef is SqlRawColumnReference 
                    ? (SqlColumnReferenceBase)new SqlRawColumnReference(realSource.Reference, colRef.ColumnName)
                    : new SqlColumnReference(realSource.Reference, colRef.ColumnName, colRef.PropertyName);
                    
                builder.AppendSegment(new SqlSegment(segment.Type, mappedColumn, segment.RenderMode, segment.Tag));
                continue;
            }

            // 5. Passthrough
            builder.AppendSegment(segment);
        }

        return builder;
    }
}