namespace SqlInterpol;

public static partial class SqlBuilderExtensions
{
    public static ISqlEntity<T> AddEntity<T>(this SqlBuilder builder, string? name = null, string? schema = null, string? alias = null)
        => ((ISqlEntityRegistry)builder).RegisterEntity<T>(name, schema, alias);

    public static SqlQueryBuilder<T1> Entity<T1>(this SqlBuilder builder, string? name = null, string? schema = null, string? alias = null)
        => new(builder, name, schema, alias);

    public static ISqlQuery<T1> Query<T1>(this SqlBuilder builder, Action<ISqlEntity<T1>> body)
        => builder.Entity<T1>().Query(body);

    public static ISqlQuery<T1> Query<T1, T2>(this SqlBuilder builder, Action<ISqlEntity<T1>, ISqlEntity<T2>> body)
        => builder.Entity<T1>().Entity<T2>().Query(body);

    public static ISqlQuery<T1> Query<T1, T2, T3>(this SqlBuilder builder, Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>> body)
        => builder.Entity<T1>().Entity<T2>().Entity<T3>().Query(body);

    public static ISqlQuery<T1> Query<T1, T2, T3, T4>(this SqlBuilder builder, Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>> body)
        => builder.Entity<T1>().Entity<T2>().Entity<T3>().Entity<T4>().Query(body);

    public static ISqlQuery<T1> Query<T1, T2, T3, T4, T5>(this SqlBuilder builder, Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>, ISqlEntity<T5>> body)
        => builder.Entity<T1>().Entity<T2>().Entity<T3>().Entity<T4>().Entity<T5>().Query(body);
}

public readonly struct SqlQueryBuilder<T1>(SqlBuilder builder, string? name, string? schema, string? alias)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlEntity<T1> Entity1 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T1>(name, schema, alias);

    public SqlQueryBuilder<T1, T2> Entity<T2>(string? name = null, string? schema = null, string? alias = null)
    {
        return new SqlQueryBuilder<T1, T2>(Builder, Entity1, name, schema, alias);
    }

    public ISqlQuery<T1> Query(Action<ISqlEntity<T1>> body)
    {
        var e1 = Entity1;
        var innerQuery = Builder.Query(() => body(e1));
        return new SqlQuery<T1>(Builder, innerQuery, e1.Reference.Alias);
    }
}

public readonly struct SqlQueryBuilder<T1, T2>(SqlBuilder builder, ISqlEntity<T1> e1, string? name, string? schema, string? alias)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlEntity<T1> Entity1 { get; } = e1;
    public ISqlEntity<T2> Entity2 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T2>(name, schema, alias);

    public SqlQueryBuilder<T1, T2, T3> Entity<T3>(string? name = null, string? schema = null, string? alias = null)
    {
        return new SqlQueryBuilder<T1, T2, T3>(Builder, Entity1, Entity2, name, schema, alias);
    }

    public ISqlQuery<T1> Query(Action<ISqlEntity<T1>, ISqlEntity<T2>> body)
    {
        var e1 = Entity1;
        var e2 = Entity2;
        var innerQuery = Builder.Query(() => body(e1, e2));
        return new SqlQuery<T1>(Builder, innerQuery, e1.Reference.Alias);
    }
}

public readonly struct SqlQueryBuilder<T1, T2, T3>(SqlBuilder builder, ISqlEntity<T1> e1, ISqlEntity<T2> e2, string? name, string? schema, string? alias)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlEntity<T1> Entity1 { get; } = e1;
    public ISqlEntity<T2> Entity2 { get; } = e2;
    public ISqlEntity<T3> Entity3 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T3>(name, schema, alias);

    public SqlQueryBuilder<T1, T2, T3, T4> Entity<T4>(string? name = null, string? schema = null, string? alias = null)
        => new(Builder, Entity1, Entity2, Entity3, name, schema, alias);

    public ISqlQuery<T1> Query(Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>> body)
    {
        var e1 = Entity1;
        var e2 = Entity2;
        var e3 = Entity3;
        var innerQuery = Builder.Query(() => body(e1, e2, e3));
        return new SqlQuery<T1>(Builder, innerQuery, e1.Reference.Alias);
    }
}

public readonly struct SqlQueryBuilder<T1, T2, T3, T4>(SqlBuilder builder, ISqlEntity<T1> e1, ISqlEntity<T2> e2, ISqlEntity<T3> e3, string? name, string? schema, string? alias)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlEntity<T1> Entity1 { get; } = e1;
    public ISqlEntity<T2> Entity2 { get; } = e2;
    public ISqlEntity<T3> Entity3 { get; } = e3;
    public ISqlEntity<T4> Entity4 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T4>(name, schema, alias);

    public SqlQueryBuilder<T1, T2, T3, T4, T5> Entity<T5>(string? name = null, string? schema = null, string? alias = null)
        => new(Builder, Entity1, Entity2, Entity3, Entity4, name, schema, alias);

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

public readonly struct SqlQueryBuilder<T1, T2, T3, T4, T5>(SqlBuilder builder, ISqlEntity<T1> e1, ISqlEntity<T2> e2, ISqlEntity<T3> e3, ISqlEntity<T4> e4, string? name, string? schema, string? alias)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlEntity<T1> Entity1 { get; } = e1;
    public ISqlEntity<T2> Entity2 { get; } = e2;
    public ISqlEntity<T3> Entity3 { get; } = e3;
    public ISqlEntity<T4> Entity4 { get; } = e4;
    public ISqlEntity<T5> Entity5 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T5>(name, schema, alias);

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