using System.Collections.Generic;

namespace SqlInterpol;

/// <summary>
/// Provides fluent extension methods to append optimized, globally cached CRUD operations.
/// </summary>
public static partial class SqlBuilderExtensions
{
    /// <summary>
    /// Appends a highly-optimized, globally cached INSERT statement natively formatted for the active dialect. 
    /// Supports a single item, comma-separated inline items, or a pre-allocated array via params.
    /// </summary>
    public static SqlBuilder AppendInsert<TEntity, TDto>(
        this SqlBuilder builder, 
        TEntity entity, 
        params TDto[] payloads)
    {
        var template = SqlCrudTemplateCache.GetInsertTemplate<TEntity, TDto>(builder.Context.Dialect);
        return builder.Append(template, payloads);
    }

    /// <summary>
    /// Appends a highly-optimized, globally cached INSERT statement natively formatted for the active dialect. 
    /// Supports both single-item inserts and multi-row bulk inserts seamlessly.
    /// </summary>
    public static SqlBuilder AppendInsert<TEntity, TDto>(
        this SqlBuilder builder, 
        TEntity entity, 
        IEnumerable<TDto> payloads)
    {
        var template = SqlCrudTemplateCache.GetInsertTemplate<TEntity, TDto>(builder.Context.Dialect);
        return builder.Append(template, payloads);
    }
}