// =========================================================================
// TODO (v2.0): ENTIRE FILE IS DEAD CODE. 
// Remove this file completely once the legacy lambda syntax is fully deprecated.
// =========================================================================

namespace SqlInterpol;

/// <summary>
/// Extension methods for <see cref="SqlBuilder"/> that provide strongly-typed entity registration
/// and scoped query construction with up to five concurrent entity types.
/// </summary>
public static partial class SqlBuilderExtensions
{
    /// <summary>
    /// Registers a typed entity with the builder and returns its <see cref="ISqlEntity{T}"/> handle.
    /// The entity's physical table name, schema, and alias are resolved from metadata unless overridden.
    /// </summary>
    /// <typeparam name="T">The CLR model type to map.</typeparam>
    /// <param name="builder">The builder to register the entity with.</param>
    /// <param name="name">Overrides the physical table name; uses the value from <see cref="SqlEntityAttribute"/> or the type name when <see langword="null"/>.</param>
    /// <param name="schema">Overrides the schema; uses the value from metadata when <see langword="null"/>.</param>
    /// <param name="alias">Overrides the SQL alias; auto-generated when <see langword="null"/>.</param>
    /// <returns>The registered <see cref="ISqlEntity{T}"/> handle for use inside query lambdas.</returns>
    public static ISqlEntity<T> AddEntity<T>(this SqlBuilder builder, string? name = null, string? schema = null, string? alias = null)
        => ((ISqlEntityRegistry)builder).RegisterEntity<T>(name, schema, alias);

    /// <summary>
    /// Starts a fluent query-builder chain for a single entity type.
    /// Call <see cref="SqlQueryBuilder{T1}.Query"/> on the returned builder to define the query body.
    /// </summary>
    /// <typeparam name="T1">The primary entity type.</typeparam>
    /// <param name="builder">The owning <see cref="SqlBuilder"/>.</param>
    /// <param name="name">Overrides the physical table name.</param>
    /// <param name="schema">Overrides the schema.</param>
    /// <param name="alias">Overrides the SQL alias.</param>
    /// <returns>A <see cref="SqlQueryBuilder{T1}"/> scoped to <typeparamref name="T1"/>.</returns>
    public static SqlQueryBuilder<T1> Entity<T1>(this SqlBuilder builder, string? name = null, string? schema = null, string? alias = null)
        => new(builder, name, schema, alias);

    /// <summary>
    /// Creates a scoped <see cref="ISqlQuery{T1}"/> with a single entity type.
    /// </summary>
    /// <typeparam name="T1">The primary entity type.</typeparam>
    /// <param name="builder">The owning <see cref="SqlBuilder"/>.</param>
    /// <param name="body">The action that appends SQL to the builder using the entity handle.</param>
    /// <returns>A typed <see cref="ISqlQuery{T1}"/> capturing the SQL written inside <paramref name="body"/>.</returns>
    public static ISqlQuery<T1> Query<T1>(this SqlBuilder builder, Action<ISqlEntity<T1>> body)
        => builder.Entity<T1>().Query(body);

    /// <summary>
    /// Creates a scoped <see cref="ISqlQuery{T1}"/> with two entity types.
    /// </summary>
    /// <typeparam name="T1">The primary entity type.</typeparam>
    /// <typeparam name="T2">The secondary entity type.</typeparam>
    /// <param name="builder">The owning <see cref="SqlBuilder"/>.</param>
    /// <param name="body">The action that appends SQL to the builder using both entity handles.</param>
    /// <returns>A typed <see cref="ISqlQuery{T1}"/> capturing the SQL written inside <paramref name="body"/>.</returns>
    public static ISqlQuery<T1> Query<T1, T2>(this SqlBuilder builder, Action<ISqlEntity<T1>, ISqlEntity<T2>> body)
        => builder.Entity<T1>().Entity<T2>().Query(body);

    /// <summary>
    /// Creates a scoped <see cref="ISqlQuery{T1}"/> with three entity types.
    /// </summary>
    /// <typeparam name="T1">The primary entity type.</typeparam>
    /// <typeparam name="T2">The second entity type.</typeparam>
    /// <typeparam name="T3">The third entity type.</typeparam>
    /// <param name="builder">The owning <see cref="SqlBuilder"/>.</param>
    /// <param name="body">The action that appends SQL to the builder using all three entity handles.</param>
    /// <returns>A typed <see cref="ISqlQuery{T1}"/> capturing the SQL written inside <paramref name="body"/>.</returns>
    public static ISqlQuery<T1> Query<T1, T2, T3>(this SqlBuilder builder, Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>> body)
        => builder.Entity<T1>().Entity<T2>().Entity<T3>().Query(body);

    /// <summary>
    /// Creates a scoped <see cref="ISqlQuery{T1}"/> with four entity types.
    /// </summary>
    /// <typeparam name="T1">The primary entity type.</typeparam>
    /// <typeparam name="T2">The second entity type.</typeparam>
    /// <typeparam name="T3">The third entity type.</typeparam>
    /// <typeparam name="T4">The fourth entity type.</typeparam>
    /// <param name="builder">The owning <see cref="SqlBuilder"/>.</param>
    /// <param name="body">The action that appends SQL to the builder using all four entity handles.</param>
    /// <returns>A typed <see cref="ISqlQuery{T1}"/> capturing the SQL written inside <paramref name="body"/>.</returns>
    public static ISqlQuery<T1> Query<T1, T2, T3, T4>(this SqlBuilder builder, Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>> body)
        => builder.Entity<T1>().Entity<T2>().Entity<T3>().Entity<T4>().Query(body);

    /// <summary>
    /// Creates a scoped <see cref="ISqlQuery{T1}"/> with five entity types.
    /// </summary>
    /// <typeparam name="T1">The primary entity type.</typeparam>
    /// <typeparam name="T2">The second entity type.</typeparam>
    /// <typeparam name="T3">The third entity type.</typeparam>
    /// <typeparam name="T4">The fourth entity type.</typeparam>
    /// <typeparam name="T5">The fifth entity type.</typeparam>
    /// <param name="builder">The owning <see cref="SqlBuilder"/>.</param>
    /// <param name="body">The action that appends SQL to the builder using all five entity handles.</param>
    /// <returns>A typed <see cref="ISqlQuery{T1}"/> capturing the SQL written inside <paramref name="body"/>.</returns>
    public static ISqlQuery<T1> Query<T1, T2, T3, T4, T5>(this SqlBuilder builder, Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>, ISqlEntity<T5>> body)
        => builder.Entity<T1>().Entity<T2>().Entity<T3>().Entity<T4>().Entity<T5>().Query(body);
}

/// <summary>
/// An intermediate fluent builder that holds a registered <typeparamref name="T1"/> entity and
/// allows adding further entity types or finalizing the query.
/// </summary>
/// <typeparam name="T1">The primary entity type.</typeparam>
public readonly struct SqlQueryBuilder<T1>(SqlBuilder builder, string? name, string? schema, string? alias)
{
    /// <summary>Gets the underlying <see cref="SqlBuilder"/>.</summary>
    public SqlBuilder Builder { get; } = builder;
    /// <summary>Gets the registered handle for the primary entity.</summary>
    public ISqlEntity<T1> Entity1 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T1>(name, schema, alias);

    /// <summary>
    /// Adds a second entity type to the query scope.
    /// </summary>
    /// <typeparam name="T2">The second entity type to register.</typeparam>
    /// <param name="name">Overrides the physical table name.</param>
    /// <param name="schema">Overrides the schema.</param>
    /// <param name="alias">Overrides the SQL alias.</param>
    /// <returns>A <see cref="SqlQueryBuilder{T1, T2}"/> scoped to both entity types.</returns>
    public SqlQueryBuilder<T1, T2> Entity<T2>(string? name = null, string? schema = null, string? alias = null)
    {
        return new SqlQueryBuilder<T1, T2>(Builder, Entity1, name, schema, alias);
    }

    /// <summary>
    /// Captures the SQL written by <paramref name="body"/> into a scoped <see cref="ISqlQuery{T1}"/>.
    /// </summary>
    /// <param name="body">The action that appends SQL to the builder using the entity handle.</param>
    /// <returns>A typed <see cref="ISqlQuery{T1}"/> capturing the SQL written inside <paramref name="body"/>.</returns>
    public ISqlQuery<T1> Query(Action<ISqlEntity<T1>> body)
    {
        var e1 = Entity1;
        var innerQuery = Builder.Query(() => body(e1));
        return new SqlQuery<T1>(Builder, innerQuery, e1.Reference.Alias);
    }
}

/// <summary>
/// An intermediate fluent builder scoped to two entity types.
/// </summary>
/// <typeparam name="T1">The primary entity type.</typeparam>
/// <typeparam name="T2">The second entity type.</typeparam>
public readonly struct SqlQueryBuilder<T1, T2>(SqlBuilder builder, ISqlEntity<T1> e1, string? name, string? schema, string? alias)
{
    /// <summary>Gets the underlying <see cref="SqlBuilder"/>.</summary>
    public SqlBuilder Builder { get; } = builder;
    /// <summary>Gets the registered handle for the primary entity.</summary>
    public ISqlEntity<T1> Entity1 { get; } = e1;
    /// <summary>Gets the registered handle for the second entity.</summary>
    public ISqlEntity<T2> Entity2 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T2>(name, schema, alias);

    /// <summary>
    /// Adds a third entity type to the query scope.
    /// </summary>
    /// <typeparam name="T3">The third entity type to register.</typeparam>
    /// <param name="name">Overrides the physical table name.</param>
    /// <param name="schema">Overrides the schema.</param>
    /// <param name="alias">Overrides the SQL alias.</param>
    /// <returns>A <see cref="SqlQueryBuilder{T1, T2, T3}"/> scoped to all three entity types.</returns>
    public SqlQueryBuilder<T1, T2, T3> Entity<T3>(string? name = null, string? schema = null, string? alias = null)
    {
        return new SqlQueryBuilder<T1, T2, T3>(Builder, Entity1, Entity2, name, schema, alias);
    }

    /// <summary>
    /// Captures the SQL written by <paramref name="body"/> into a scoped <see cref="ISqlQuery{T1}"/>.
    /// </summary>
    /// <param name="body">The action that appends SQL to the builder using both entity handles.</param>
    /// <returns>A typed <see cref="ISqlQuery{T1}"/> capturing the SQL written inside <paramref name="body"/>.</returns>
    public ISqlQuery<T1> Query(Action<ISqlEntity<T1>, ISqlEntity<T2>> body)
    {
        var e1 = Entity1;
        var e2 = Entity2;
        var innerQuery = Builder.Query(() => body(e1, e2));
        return new SqlQuery<T1>(Builder, innerQuery, e1.Reference.Alias);
    }
}

/// <summary>
/// An intermediate fluent builder scoped to three entity types.
/// </summary>
/// <typeparam name="T1">The primary entity type.</typeparam>
/// <typeparam name="T2">The second entity type.</typeparam>
/// <typeparam name="T3">The third entity type.</typeparam>
public readonly struct SqlQueryBuilder<T1, T2, T3>(SqlBuilder builder, ISqlEntity<T1> e1, ISqlEntity<T2> e2, string? name, string? schema, string? alias)
{
    /// <summary>Gets the underlying <see cref="SqlBuilder"/>.</summary>
    public SqlBuilder Builder { get; } = builder;
    /// <summary>Gets the registered handle for the primary entity.</summary>
    public ISqlEntity<T1> Entity1 { get; } = e1;
    /// <summary>Gets the registered handle for the second entity.</summary>
    public ISqlEntity<T2> Entity2 { get; } = e2;
    /// <summary>Gets the registered handle for the third entity.</summary>
    public ISqlEntity<T3> Entity3 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T3>(name, schema, alias);

    /// <summary>
    /// Adds a fourth entity type to the query scope.
    /// </summary>
    /// <typeparam name="T4">The fourth entity type to register.</typeparam>
    /// <param name="name">Overrides the physical table name.</param>
    /// <param name="schema">Overrides the schema.</param>
    /// <param name="alias">Overrides the SQL alias.</param>
    /// <returns>A <see cref="SqlQueryBuilder{T1, T2, T3, T4}"/> scoped to all four entity types.</returns>
    public SqlQueryBuilder<T1, T2, T3, T4> Entity<T4>(string? name = null, string? schema = null, string? alias = null)
        => new(Builder, Entity1, Entity2, Entity3, name, schema, alias);

    /// <summary>
    /// Captures the SQL written by <paramref name="body"/> into a scoped <see cref="ISqlQuery{T1}"/>.
    /// </summary>
    /// <param name="body">The action that appends SQL to the builder using all three entity handles.</param>
    /// <returns>A typed <see cref="ISqlQuery{T1}"/> capturing the SQL written inside <paramref name="body"/>.</returns>
    public ISqlQuery<T1> Query(Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>> body)
    {
        var e1 = Entity1;
        var e2 = Entity2;
        var e3 = Entity3;
        var innerQuery = Builder.Query(() => body(e1, e2, e3));
        return new SqlQuery<T1>(Builder, innerQuery, e1.Reference.Alias);
    }
}

/// <summary>
/// An intermediate fluent builder scoped to four entity types.
/// </summary>
/// <typeparam name="T1">The primary entity type.</typeparam>
/// <typeparam name="T2">The second entity type.</typeparam>
/// <typeparam name="T3">The third entity type.</typeparam>
/// <typeparam name="T4">The fourth entity type.</typeparam>
public readonly struct SqlQueryBuilder<T1, T2, T3, T4>(SqlBuilder builder, ISqlEntity<T1> e1, ISqlEntity<T2> e2, ISqlEntity<T3> e3, string? name, string? schema, string? alias)
{
    /// <summary>Gets the underlying <see cref="SqlBuilder"/>.</summary>
    public SqlBuilder Builder { get; } = builder;
    /// <summary>Gets the registered handle for the primary entity.</summary>
    public ISqlEntity<T1> Entity1 { get; } = e1;
    /// <summary>Gets the registered handle for the second entity.</summary>
    public ISqlEntity<T2> Entity2 { get; } = e2;
    /// <summary>Gets the registered handle for the third entity.</summary>
    public ISqlEntity<T3> Entity3 { get; } = e3;
    /// <summary>Gets the registered handle for the fourth entity.</summary>
    public ISqlEntity<T4> Entity4 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T4>(name, schema, alias);

    /// <summary>
    /// Adds a fifth entity type to the query scope.
    /// </summary>
    /// <typeparam name="T5">The fifth entity type to register.</typeparam>
    /// <param name="name">Overrides the physical table name.</param>
    /// <param name="schema">Overrides the schema.</param>
    /// <param name="alias">Overrides the SQL alias.</param>
    /// <returns>A <see cref="SqlQueryBuilder{T1, T2, T3, T4, T5}"/> scoped to all five entity types.</returns>
    public SqlQueryBuilder<T1, T2, T3, T4, T5> Entity<T5>(string? name = null, string? schema = null, string? alias = null)
        => new(Builder, Entity1, Entity2, Entity3, Entity4, name, schema, alias);

    /// <summary>
    /// Captures the SQL written by <paramref name="body"/> into a scoped <see cref="ISqlQuery{T1}"/>.
    /// </summary>
    /// <param name="body">The action that appends SQL to the builder using all four entity handles.</param>
    /// <returns>A typed <see cref="ISqlQuery{T1}"/> capturing the SQL written inside <paramref name="body"/>.</returns>
    public ISqlQuery<T1> Query(Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>> body)
    {
        var e1 = Entity1;
        var e2 = Entity2;
        var e3 = Entity3;
        var e4 = Entity4;
        var innerQuery = Builder.Query(() => body(e1, e2, e3, e4));
        return new SqlQuery<T1>(Builder, innerQuery, e1.Reference.Alias);
    }
}

/// <summary>
/// An intermediate fluent builder scoped to five entity types.
/// </summary>
/// <typeparam name="T1">The primary entity type.</typeparam>
/// <typeparam name="T2">The second entity type.</typeparam>
/// <typeparam name="T3">The third entity type.</typeparam>
/// <typeparam name="T4">The fourth entity type.</typeparam>
/// <typeparam name="T5">The fifth entity type.</typeparam>
public readonly struct SqlQueryBuilder<T1, T2, T3, T4, T5>(SqlBuilder builder, ISqlEntity<T1> e1, ISqlEntity<T2> e2, ISqlEntity<T3> e3, ISqlEntity<T4> e4, string? name, string? schema, string? alias)
{
    /// <summary>Gets the underlying <see cref="SqlBuilder"/>.</summary>
    public SqlBuilder Builder { get; } = builder;
    /// <summary>Gets the registered handle for the primary entity.</summary>
    public ISqlEntity<T1> Entity1 { get; } = e1;
    /// <summary>Gets the registered handle for the second entity.</summary>
    public ISqlEntity<T2> Entity2 { get; } = e2;
    /// <summary>Gets the registered handle for the third entity.</summary>
    public ISqlEntity<T3> Entity3 { get; } = e3;
    /// <summary>Gets the registered handle for the fourth entity.</summary>
    public ISqlEntity<T4> Entity4 { get; } = e4;
    /// <summary>Gets the registered handle for the fifth entity.</summary>
    public ISqlEntity<T5> Entity5 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T5>(name, schema, alias);

    /// <summary>
    /// Captures the SQL written by <paramref name="body"/> into a scoped <see cref="ISqlQuery{T1}"/>.
    /// </summary>
    /// <param name="body">The action that appends SQL to the builder using all five entity handles.</param>
    /// <returns>A typed <see cref="ISqlQuery{T1}"/> capturing the SQL written inside <paramref name="body"/>.</returns>
    public ISqlQuery<T1> Query(Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>, ISqlEntity<T5>> body)
    {
        var e1 = Entity1;
        var e2 = Entity2;
        var e3 = Entity3;
        var e4 = Entity4;
        var e5 = Entity5;
        var innerQuery = Builder.Query(() => body(e1, e2, e3, e4, e5));
        return new SqlQuery<T1>(Builder, innerQuery, e1.Reference.Alias);
    }
}