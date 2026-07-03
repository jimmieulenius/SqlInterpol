// using System.Linq.Expressions;

// namespace SqlInterpol;

// /// <summary>
// /// Provides fluent extension methods to append optimized, globally cached CRUD operations.
// /// </summary>
// public static partial class SqlBuilderExtensions
// {
//     /// <summary>
//     /// Appends a highly-optimized, globally cached INSERT statement. 
//     /// Supports both full entities and partial DTOs seamlessly via type inference.
//     /// </summary>
//     public static SqlBuilder AppendInsert<TEntity, TDto>(
//         this SqlBuilder builder, 
//         ISqlEntityBase<TEntity> entity, 
//         TDto payload)
//     {
//         return builder.Append(SqlTemplate.Insert<TEntity, TDto>(), entity, payload);
//     }

//     /// <summary>
//     /// Appends a highly-optimized, globally cached UPDATE statement utilizing a key lambda selector.
//     /// Supports both full entities and partial DTOs seamlessly via type inference.
//     /// </summary>
//     public static SqlBuilder AppendUpdate<TEntity, TDto>(
//         this SqlBuilder builder, 
//         ISqlEntityBase<TEntity> entity, 
//         TDto payload, 
//         Expression<Func<TDto, object>> keySelector)
//     {
//         return builder.Append(SqlTemplate.Update<TEntity, TDto>(keySelector), entity, payload);
//     }

//     /// <summary>
//     /// Appends a highly-optimized, globally cached UPDATE statement utilizing explicitly named key property strings.
//     /// </summary>
//     public static SqlBuilder AppendUpdate<TEntity, TDto>(
//         this SqlBuilder builder, 
//         ISqlEntityBase<TEntity> entity, 
//         TDto payload, 
//         params string[] keyPropertyNames)
//     {
//         return builder.Append(SqlTemplate.Update<TEntity, TDto>(keyPropertyNames), entity, payload);
//     }

//     /// <summary>
//     /// Appends a globally cached universal UPSERT statement utilizing a key lambda selector.
//     /// Supports both full entities and partial DTOs seamlessly via type inference.
//     /// </summary>
//     public static SqlBuilder AppendUpsert<TEntity, TDto>(
//         this SqlBuilder builder, 
//         ISqlEntityBase<TEntity> entity, 
//         TDto payload, 
//         Expression<Func<TDto, object>> keySelector)
//     {
//         return builder.Append(SqlTemplate.Upsert<TEntity, TDto>(keySelector), entity, payload);
//     }

//     /// <summary>
//     /// Appends a globally cached universal UPSERT statement utilizing explicitly named key property strings.
//     /// </summary>
//     public static SqlBuilder AppendUpsert<TEntity, TDto>(
//         this SqlBuilder builder, 
//         ISqlEntityBase<TEntity> entity, 
//         TDto payload, 
//         params string[] keyPropertyNames)
//     {
//         return builder.Append(SqlTemplate.Upsert<TEntity, TDto>(keyPropertyNames), entity, payload);
//     }

//     /// <summary>
//     /// Appends a highly-optimized, globally cached DELETE statement utilizing a key lambda selector.
//     /// </summary>
//     public static SqlBuilder AppendDelete<TEntity, TDto>(
//         this SqlBuilder builder, 
//         ISqlEntityBase<TEntity> entity, 
//         TDto payload, 
//         Expression<Func<TDto, object>> keySelector)
//     {
//         return builder.Append(SqlTemplate.Delete<TEntity, TDto>(keySelector), entity, payload);
//     }

//     /// <summary>
//     /// Appends a highly-optimized, globally cached DELETE statement utilizing explicitly named key property strings.
//     /// </summary>
//     public static SqlBuilder AppendDelete<TEntity, TDto>(
//         this SqlBuilder builder, 
//         ISqlEntityBase<TEntity> entity, 
//         TDto payload, 
//         params string[] keyPropertyNames)
//     {
//         return builder.Append(SqlTemplate.Delete<TEntity, TDto>(keyPropertyNames), entity, payload);
//     }
// }