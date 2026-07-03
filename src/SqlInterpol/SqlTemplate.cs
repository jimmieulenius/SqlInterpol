// using System.Collections.Concurrent;
// using System.Linq.Expressions;
// using SqlInterpol.Parsing;

// namespace SqlInterpol;

// /// <summary>
// /// The base class for all pre-compiled SQL templates, and the factory for creating them.
// /// Templates are statically cached for O(1) allocation-free performance.
// /// </summary>
// public abstract class SqlTemplate(Lazy<IReadOnlyList<SqlSegment>> segments)
// {
//     /// <summary>
//     /// The pre-compiled sequence of SQL segments. 
//     /// This is evaluated lazily the first time the template is used.
//     /// </summary>
//     internal IReadOnlyList<SqlSegment> Segments => segments.Value;

//     /// <summary>Creates a custom pre-compiled SQL template requiring a single entity context.</summary>
//     /// <typeparam name="T">The model type for the template's placeholder entity.</typeparam>
//     /// <param name="definition">A delegate that defines the structural layout using standard SQL string interpolation blocks.</param>
//     /// <returns>A pre-compiled <see cref="SqlTemplate{T}"/> ready for high-performance execution.</returns>
//     public static SqlTemplate<T> Create<T>(Action<SqlBuilder, ISqlEntityBase<T>> definition)
//     {
//         var entity = new SqlTable<T>($"__dummy_{Guid.NewGuid():N}");
//         var lazySegments = new Lazy<IReadOnlyList<SqlSegment>>(() =>
//         {
//             var db = new SqlBuilder(new Dialects.AnsiSqlDialect()); 
//             definition(db, entity);
//             return db.Segments;
//         });

//         return new SqlTemplate<T>(entity, lazySegments);
//     }

//     public static SqlTemplate<T1, T2> Create<T1, T2>(Action<SqlBuilder, ISqlEntityBase<T1>, ISqlEntityBase<T2>> definition)
//     {
//         var entity1 = new SqlTable<T1>($"__dummy_{Guid.NewGuid():N}");
//         var entity2 = new SqlTable<T2>($"__dummy_{Guid.NewGuid():N}");
//         var lazySegments = new Lazy<IReadOnlyList<SqlSegment>>(() =>
//         {
//             var db = new SqlBuilder(new Dialects.AnsiSqlDialect()); 
//             definition(db, entity1, entity2);
//             return db.Segments;
//         });

//         return new SqlTemplate<T1, T2>(entity1, entity2, lazySegments);
//     }

//     public static SqlTemplate<T1, T2, T3> Create<T1, T2, T3>(Action<SqlBuilder, ISqlEntityBase<T1>, ISqlEntityBase<T2>, ISqlEntityBase<T3>> definition)
//     {
//         var entity1 = new SqlTable<T1>($"__dummy_{Guid.NewGuid():N}");
//         var entity2 = new SqlTable<T2>($"__dummy_{Guid.NewGuid():N}");
//         var entity3 = new SqlTable<T3>($"__dummy_{Guid.NewGuid():N}");
//         var lazySegments = new Lazy<IReadOnlyList<SqlSegment>>(() =>
//         {
//             var db = new SqlBuilder(new Dialects.AnsiSqlDialect()); 
//             definition(db, entity1, entity2, entity3);
//             return db.Segments;
//         });

//         return new SqlTemplate<T1, T2, T3>(entity1, entity2, entity3, lazySegments);
//     }

//     public static SqlTemplate<T1, T2, T3, T4> Create<T1, T2, T3, T4>(Action<SqlBuilder, ISqlEntityBase<T1>, ISqlEntityBase<T2>, ISqlEntityBase<T3>, ISqlEntityBase<T4>> definition)
//     {
//         var entity1 = new SqlTable<T1>($"__dummy_{Guid.NewGuid():N}");
//         var entity2 = new SqlTable<T2>($"__dummy_{Guid.NewGuid():N}");
//         var entity3 = new SqlTable<T3>($"__dummy_{Guid.NewGuid():N}");
//         var entity4 = new SqlTable<T4>($"__dummy_{Guid.NewGuid():N}");
//         var lazySegments = new Lazy<IReadOnlyList<SqlSegment>>(() =>
//         {
//             var db = new SqlBuilder(new Dialects.AnsiSqlDialect()); 
//             definition(db, entity1, entity2, entity3, entity4);
//             return db.Segments;
//         });

//         return new SqlTemplate<T1, T2, T3, T4>(entity1, entity2, entity3, entity4, lazySegments);
//     }

//     public static SqlTemplate<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(Action<SqlBuilder, ISqlEntityBase<T1>, ISqlEntityBase<T2>, ISqlEntityBase<T3>, ISqlEntityBase<T4>, ISqlEntityBase<T5>> definition)
//     {
//         var entity1 = new SqlTable<T1>($"__dummy_{Guid.NewGuid():N}");
//         var entity2 = new SqlTable<T2>($"__dummy_{Guid.NewGuid():N}");
//         var entity3 = new SqlTable<T3>($"__dummy_{Guid.NewGuid():N}");
//         var entity4 = new SqlTable<T4>($"__dummy_{Guid.NewGuid():N}");
//         var entity5 = new SqlTable<T5>($"__dummy_{Guid.NewGuid():N}");
//         var lazySegments = new Lazy<IReadOnlyList<SqlSegment>>(() =>
//         {
//             var db = new SqlBuilder(new Dialects.AnsiSqlDialect()); 
//             definition(db, entity1, entity2, entity3, entity4, entity5);
//             return db.Segments;
//         });

//         return new SqlTemplate<T1, T2, T3, T4, T5>(entity1, entity2, entity3, entity4, entity5, lazySegments);
//     }

//     /// <summary>Gets a fully cached INSERT template restricted to the DTO's properties.</summary>
//     /// <typeparam name="TEntity">The target table model type.</typeparam>
//     /// <typeparam name="TDto">The data transfer object type defining properties to persist.</typeparam>
//     /// <returns>The cached INSERT template.</returns>
//     public static SqlTemplate<TEntity> Insert<TEntity, TDto>() 
//         => InsertTemplateCache<TEntity, TDto>.Template;

//     /// <summary>Gets a fully cached SELECT (Get By Key) template matching designated key properties.</summary>
//     /// <typeparam name="TEntity">The target table model type to read from.</typeparam>
//     /// <typeparam name="TDto">The data transfer object type mapping the data structure.</typeparam>
//     /// <param name="keySelector">A lambda expression targeting one or more key properties used for filtering.</param>
//     /// <returns>The cached SELECT template.</returns>
//     public static SqlTemplate<TEntity> Select<TEntity, TDto>(Expression<Func<TDto, object>> keySelector) 
//         => SelectTemplateCache<TEntity, TDto>.GetTemplate(SqlExpressionHelper.GetPropertyNames(keySelector));

//     /// <summary>Gets a fully cached SELECT (Get By Key) template using explicitly named key properties.</summary>
//     /// <typeparam name="TEntity">The target table model type to read from.</typeparam>
//     /// <typeparam name="TDto">The data transfer object type mapping the data structure.</typeparam>
//     /// <param name="keyPropertyNames">The array of property names acting as constraints for filtering.</param>
//     /// <returns>The cached SELECT template.</returns>
//     public static SqlTemplate<TEntity> Select<TEntity, TDto>(params string[] keyPropertyNames) 
//         => SelectTemplateCache<TEntity, TDto>.GetTemplate(keyPropertyNames);

//     /// <summary>Gets a fully cached UPDATE template restricted to the DTO's SET and WHERE properties.</summary>
//     /// <typeparam name="TEntity">The target table model type.</typeparam>
//     /// <typeparam name="TDto">The data transfer object type containing fields to modify.</typeparam>
//     /// <param name="keySelector">A lambda expression targeting properties serving as primary key mapping criteria.</param>
//     /// <returns>The cached UPDATE template.</returns>
//     public static SqlTemplate<TEntity> Update<TEntity, TDto>(Expression<Func<TDto, object>> keySelector) 
//         => UpdateTemplateCache<TEntity, TDto>.GetTemplate(SqlExpressionHelper.GetPropertyNames(keySelector));

//     /// <summary>Gets a fully cached UPDATE template using explicitly named key properties.</summary>
//     /// <typeparam name="TEntity">The target table model type.</typeparam>
//     /// <typeparam name="TDto">The data transfer object type containing fields to modify.</typeparam>
//     /// <param name="keyPropertyNames">The explicit property names representing target filter identifiers.</param>
//     /// <returns>The cached UPDATE template.</returns>
//     public static SqlTemplate<TEntity> Update<TEntity, TDto>(params string[] keyPropertyNames) 
//         => UpdateTemplateCache<TEntity, TDto>.GetTemplate(keyPropertyNames);

//     /// <summary>Gets a fully cached universal UPSERT template restricted to the DTO's properties.</summary>
//     /// <typeparam name="TEntity">The target table model type.</typeparam>
//     /// <typeparam name="TDto">The data transfer object type defining data elements.</typeparam>
//     /// <param name="keySelector">A lambda expression targeting conflict target properties.</param>
//     /// <returns>The cached UPSERT template.</returns>
//     public static SqlTemplate<TEntity> Upsert<TEntity, TDto>(Expression<Func<TDto, object>> keySelector) 
//         => UpsertTemplateCache<TEntity, TDto>.GetTemplate(SqlExpressionHelper.GetPropertyNames(keySelector));

//     /// <summary>Gets a fully cached universal UPSERT template using explicitly named key properties.</summary>
//     /// <typeparam name="TEntity">The target table model type.</typeparam>
//     /// <typeparam name="TDto">The data transfer object type defining data elements.</typeparam>
//     /// <param name="keyPropertyNames">The explicit property names mapping conflicting constraints.</param>
//     /// <returns>The cached UPSERT template.</returns>
//     public static SqlTemplate<TEntity> Upsert<TEntity, TDto>(params string[] keyPropertyNames) 
//         => UpsertTemplateCache<TEntity, TDto>.GetTemplate(keyPropertyNames);

//     /// <summary>Gets a globally cached DELETE template restricted to a DTO payload's key matching properties.</summary>
//     /// <typeparam name="TEntity">The target table model type.</typeparam>
//     /// <typeparam name="TDto">The filter payload model type.</typeparam>
//     /// <param name="keySelector">A lambda expression targeting conditional evaluation filter elements.</param>
//     /// <returns>The cached DELETE template.</returns>
//     public static SqlTemplate<TEntity> Delete<TEntity, TDto>(Expression<Func<TDto, object>> keySelector) 
//         => DeleteTemplateCache<TEntity, TDto>.GetTemplate(SqlExpressionHelper.GetPropertyNames(keySelector));

//     /// <summary>Gets a globally cached DELETE template using explicitly named key properties.</summary>
//     /// <typeparam name="TEntity">The target table model type.</typeparam>
//     /// <typeparam name="TDto">The filter payload model type.</typeparam>
//     /// <param name="keyPropertyNames">The explicit key criteria property descriptors.</param>
//     /// <returns>The cached DELETE template.</returns>
//     public static SqlTemplate<TEntity> Delete<TEntity, TDto>(params string[] keyPropertyNames) 
//         => DeleteTemplateCache<TEntity, TDto>.GetTemplate(keyPropertyNames);

//     private static class InsertTemplateCache<TEntity, TDto>
//     {
//         public static readonly SqlTemplate<TEntity> Template = Create<TEntity>((db, e) => 
//             db.Append($$"""
//             INSERT INTO {{e}}
//             {{Sql.Expand<TDto>()}}
//             """));
//     }

//     private static class SelectTemplateCache<TEntity, TDto>
//     {
//         private static readonly ConcurrentDictionary<string, SqlTemplate<TEntity>> _cache = new(StringComparer.Ordinal);

//         public static SqlTemplate<TEntity> GetTemplate(string[] keyPropertyNames)
//         {
//             return _cache.GetOrAdd(string.Join(",", keyPropertyNames), _ => Create<TEntity>((db, e) =>
//             {
//                 db.Append($$"""
//                 SELECT {{Sql.Expand<TDto>()}}
//                 FROM {{e}}
//                 """);
                
//                 if (keyPropertyNames.Length > 0)
//                 {
//                     db.Append($$"""
                    
//                     WHERE 
//                     """);

//                     for (int i = 0; i < keyPropertyNames.Length; i++)
//                     {
//                         if (i > 0) db.Append($" AND ");
//                         db.Append($"{e[keyPropertyNames[i]]} = {Sql.Arg(keyPropertyNames[i])}");
//                     }
//                 }
//             }));
//         }
//     }

//     private static class UpdateTemplateCache<TEntity, TDto>
//     {
//         private static readonly ConcurrentDictionary<string, SqlTemplate<TEntity>> _cache = new(StringComparer.Ordinal);

//         public static SqlTemplate<TEntity> GetTemplate(string[] keyPropertyNames)
//         {
//             if (keyPropertyNames == null || keyPropertyNames.Length == 0)
//                 throw new ArgumentException("At least one key property must be specified for the UPDATE WHERE clause.");

//             return _cache.GetOrAdd(string.Join(",", keyPropertyNames), _ => Create<TEntity>((db, e) =>
//             {
//                 db.Append($$"""
//                 UPDATE {{e}}
//                 SET {{Sql.Expand<TDto>(keyPropertyNames)}}
//                 WHERE 
//                 """);

//                 for (int i = 0; i < keyPropertyNames.Length; i++)
//                 {
//                     if (i > 0) db.Append($" AND ");
//                     db.Append($"{e[keyPropertyNames[i]]} = {Sql.Arg(keyPropertyNames[i])}");
//                 }
//             }));
//         }
//     }

//     private static class UpsertTemplateCache<TEntity, TDto>
//     {
//         private static readonly ConcurrentDictionary<string, SqlTemplate<TEntity>> _cache = new(StringComparer.Ordinal);

//         public static SqlTemplate<TEntity> GetTemplate(string[] keyPropertyNames)
//         {
//             if (keyPropertyNames == null || keyPropertyNames.Length == 0)
//                 throw new ArgumentException("At least one key property must be specified for UPSERT.");

//             return _cache.GetOrAdd(string.Join(",", keyPropertyNames), _ => Create<TEntity>((db, e) =>
//             {
//                 db.Append($$"""
//                 INSERT INTO {{e}}
//                 {{Sql.Expand<TDto>(keyPropertyNames)}}
//                 ON CONFLICT (
//                 """);

//                 for (int i = 0; i < keyPropertyNames.Length; i++)
//                 {
//                     if (i > 0) db.Append($", ");
//                     db.Append($"{e[keyPropertyNames[i]]}");
//                 }

//                 db.Append($$"""
//                 )
//                 DO UPDATE SET {{Sql.Expand<TDto>(keyPropertyNames)}}
//                 """);
//             }));
//         }
//     }

//     private static class DeleteTemplateCache<TEntity, TDto>
//     {
//         private static readonly ConcurrentDictionary<string, SqlTemplate<TEntity>> _cache = new(StringComparer.Ordinal);

//         public static SqlTemplate<TEntity> GetTemplate(string[] keyPropertyNames)
//         {
//             if (keyPropertyNames == null || keyPropertyNames.Length == 0)
//                 throw new ArgumentException("At least one key property must be specified for the DELETE WHERE clause.");

//             return _cache.GetOrAdd(string.Join(",", keyPropertyNames), _ => Create<TEntity>((db, e) =>
//             {
//                 db.Append($$"""
//                 DELETE FROM {{e}}
//                 WHERE 
//                 """);

//                 for (int i = 0; i < keyPropertyNames.Length; i++)
//                 {
//                     if (i > 0)
//                     {
//                         bool isVertical = db.Context.Options.CollectionLayout == SqlCollectionLayout.Vertical;
//                         string indent = isVertical ? new string(' ', db.Context.Options.IndentSize) : "";
//                         db.Append($"{(isVertical ? $"{Environment.NewLine}AND {indent}" : " AND ")}");
//                     }
//                     db.Append($"{e[keyPropertyNames[i]]} = {Sql.Arg(keyPropertyNames[i])}");
//                 }
//             }));
//         }
//     }
// }

// /// <summary>A pre-compiled SQL template requiring one entity context.</summary>
// /// <typeparam name="T1">The model type for the template's placeholder entity.</typeparam>
// public class SqlTemplate<T1> : SqlTemplate
// {
//     internal ISqlEntityBase Entity1 { get; }
//     internal SqlTemplate(ISqlEntityBase e1, Lazy<IReadOnlyList<SqlSegment>> segments) : base(segments) => Entity1 = e1;
// }

// /// <summary>A pre-compiled SQL template requiring two entity contexts.</summary>
// /// <typeparam name="T1">The model type for the first placeholder entity.</typeparam>
// /// <typeparam name="T2">The model type for the second placeholder entity.</typeparam>
// public class SqlTemplate<T1, T2> : SqlTemplate
// {
//     internal ISqlEntityBase Entity1 { get; }
//     internal ISqlEntityBase Entity2 { get; }
//     internal SqlTemplate(ISqlEntityBase e1, ISqlEntityBase e2, Lazy<IReadOnlyList<SqlSegment>> segments) : base(segments) { Entity1 = e1; Entity2 = e2; }
// }

// /// <summary>A pre-compiled SQL template requiring three entity contexts.</summary>
// /// <typeparam name="T1">The model type for the first placeholder entity.</typeparam>
// /// <typeparam name="T2">The model type for the second placeholder entity.</typeparam>
// /// <typeparam name="T3">The model type for the third placeholder entity.</typeparam>
// public class SqlTemplate<T1, T2, T3> : SqlTemplate
// {
//     internal ISqlEntityBase Entity1 { get; }
//     internal ISqlEntityBase Entity2 { get; }
//     internal ISqlEntityBase Entity3 { get; }
//     internal SqlTemplate(ISqlEntityBase e1, ISqlEntityBase e2, ISqlEntityBase e3, Lazy<IReadOnlyList<SqlSegment>> segments) : base(segments) { Entity1 = e1; Entity2 = e2; Entity3 = e3; }
// }

// /// <summary>A pre-compiled SQL template requiring four entity contexts.</summary>
// /// <typeparam name="T1">The model type for the first placeholder entity.</typeparam>
// /// <typeparam name="T2">The model type for the second placeholder entity.</typeparam>
// /// <typeparam name="T3">The model type for the third placeholder entity.</typeparam>
// /// <typeparam name="T4">The model type for the fourth placeholder entity.</typeparam>
// public class SqlTemplate<T1, T2, T3, T4> : SqlTemplate
// {
//     internal ISqlEntityBase Entity1 { get; }
//     internal ISqlEntityBase Entity2 { get; }
//     internal ISqlEntityBase Entity3 { get; }
//     internal ISqlEntityBase Entity4 { get; }
//     internal SqlTemplate(ISqlEntityBase e1, ISqlEntityBase e2, ISqlEntityBase e3, ISqlEntityBase e4, Lazy<IReadOnlyList<SqlSegment>> segments) : base(segments) { Entity1 = e1; Entity2 = e2; Entity3 = e3; Entity4 = e4; }
// }

// /// <summary>A pre-compiled SQL template requiring five entity contexts.</summary>
// /// <typeparam name="T1">The model type for the first placeholder entity.</typeparam>
// /// <typeparam name="T2">The model type for the second placeholder entity.</typeparam>
// /// <typeparam name="T3">The model type for the third placeholder entity.</typeparam>
// /// <typeparam name="T4">The model type for the fourth placeholder entity.</typeparam>
// /// <typeparam name="T5">The model type for the fifth placeholder entity.</typeparam>
// public class SqlTemplate<T1, T2, T3, T4, T5> : SqlTemplate
// {
//     internal ISqlEntityBase Entity1 { get; }
//     internal ISqlEntityBase Entity2 { get; }
//     internal ISqlEntityBase Entity3 { get; }
//     internal ISqlEntityBase Entity4 { get; }
//     internal ISqlEntityBase Entity5 { get; }
//     internal SqlTemplate(ISqlEntityBase e1, ISqlEntityBase e2, ISqlEntityBase e3, ISqlEntityBase e4, ISqlEntityBase e5, Lazy<IReadOnlyList<SqlSegment>> segments) : base(segments) { Entity1 = e1; Entity2 = e2; Entity3 = e3; Entity4 = e4; Entity5 = e5; }
// }