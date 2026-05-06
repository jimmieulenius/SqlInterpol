namespace SqlInterpol;

public static partial class SqlBuilderExtensions
{
    extension (SqlBuilder builder)
    {
        public ISqlEntity<T> AddEntity<T>(string? name = null, string? schema = null)
            => ((ISqlEntityRegistry)builder).RegisterEntity<T>(name, schema);

        public SqlQueryBuilder<T1> Entity<T1>(string? name = null, string? schema = null)
            => new(builder, name, schema);

        public SqlBuilder Query<T1>(Action<ISqlEntity<T1>> body)
            => Entity<T1>(builder)
            .Query(body);

        public SqlBuilder Query<T1, T2>(Action<ISqlEntity<T1>, ISqlEntity<T2>> body)
            => Entity<T1>(builder)
            .Entity<T2>()
            .Query(body);

        public SqlBuilder Query<T1, T2, T3>(Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>> body)
            => Entity<T1>(builder)
            .Entity<T2>()
            .Entity<T3>()
            .Query(body);

        public SqlBuilder Query<T1, T2, T3, T4>(Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>> body)
            => Entity<T1>(builder)
            .Entity<T2>()
            .Entity<T3>()
            .Entity<T4>()
            .Query(body);

        public SqlBuilder Query<T1, T2, T3, T4, T5>(Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>, ISqlEntity<T5>> body)
            => Entity<T1>(builder)
            .Entity<T2>()
            .Entity<T3>()
            .Entity<T4>()
            .Entity<T5>()
            .Query(body);
    }
}

public readonly struct SqlQueryBuilder<T1>(SqlBuilder builder, string? name, string? schema)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlEntity<T1> Entity1 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T1>(name, schema);

    public SqlQueryBuilder<T1, T2> Entity<T2>(string? name = null, string? schema = null)
        => new(Builder, Entity1, name, schema);

    public SqlBuilder Query(Action<ISqlEntity<T1>> body)
    {
        body(Entity1);
        return Builder;
    }

    public SqlBuilder Query(Action<SqlBuilder, ISqlEntity<T1>> body)
    {
        body(Builder, Entity1);
        return Builder;
    }
}

public readonly struct SqlQueryBuilder<T1, T2>(SqlBuilder builder, ISqlEntity<T1> e1, string? name, string? schema)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlEntity<T1> Entity1 { get; } = e1;
    public ISqlEntity<T2> Entity2 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T2>(name, schema);

    public SqlQueryBuilder<T1, T2, T3> Entity<T3>(string? name = null, string? schema = null)
        => new(Builder, Entity1, Entity2, name, schema);

    public SqlBuilder Query(Action<ISqlEntity<T1>, ISqlEntity<T2>> body)
    {
        body(Entity1, Entity2);
        return Builder;
    }

    public SqlBuilder Query(Action<SqlBuilder, ISqlEntity<T1>, ISqlEntity<T2>> body)
    {
        body(Builder, Entity1, Entity2);
        return Builder;
    }
}

public readonly struct SqlQueryBuilder<T1, T2, T3>(SqlBuilder builder, ISqlEntity<T1> e1, ISqlEntity<T2> e2, string? name, string? schema)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlEntity<T1> Entity1 { get; } = e1;
    public ISqlEntity<T2> Entity2 { get; } = e2;
    public ISqlEntity<T3> Entity3 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T3>(name, schema);

    public SqlQueryBuilder<T1, T2, T3, T4> Entity<T4>(string? name = null, string? schema = null)
        => new(Builder, Entity1, Entity2, Entity3, name, schema);

    public SqlBuilder Query(Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>> body)
    {
        body(Entity1, Entity2, Entity3);
        return Builder;
    }

    public SqlBuilder Query(Action<SqlBuilder, ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>> body)
    {
        body(Builder, Entity1, Entity2, Entity3);
        return Builder;
    }
}

public readonly struct SqlQueryBuilder<T1, T2, T3, T4>(SqlBuilder builder, ISqlEntity<T1> e1, ISqlEntity<T2> e2, ISqlEntity<T3> e3, string? name, string? schema)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlEntity<T1> Entity1 { get; } = e1;
    public ISqlEntity<T2> Entity2 { get; } = e2;
    public ISqlEntity<T3> Entity3 { get; } = e3;
    public ISqlEntity<T4> Entity4 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T4>(name, schema);

    public SqlQueryBuilder<T1, T2, T3, T4, T5> Entity<T5>(string? name = null, string? schema = null)
        => new(Builder, Entity1, Entity2, Entity3, Entity4, name, schema);

    public SqlBuilder Query(Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>> body)
    {
        body(Entity1, Entity2, Entity3, Entity4);
        return Builder;
    }

    public SqlBuilder Query(Action<SqlBuilder, ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>> body)
    {
        body(Builder, Entity1, Entity2, Entity3, Entity4);
        return Builder;
    }
}

public readonly struct SqlQueryBuilder<T1, T2, T3, T4, T5>(SqlBuilder builder, ISqlEntity<T1> e1, ISqlEntity<T2> e2, ISqlEntity<T3> e3, ISqlEntity<T4> e4, string? name, string? schema)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlEntity<T1> Entity1 { get; } = e1;
    public ISqlEntity<T2> Entity2 { get; } = e2;
    public ISqlEntity<T3> Entity3 { get; } = e3;
    public ISqlEntity<T4> Entity4 { get; } = e4;
    public ISqlEntity<T5> Entity5 { get; } = ((ISqlEntityRegistry)builder).RegisterEntity<T5>(name, schema);

    public SqlBuilder Query(Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>, ISqlEntity<T5>> body)
    {
        body(Entity1, Entity2, Entity3, Entity4, Entity5);
        return Builder;
    }

    public SqlBuilder Query(Action<SqlBuilder, ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>, ISqlEntity<T5>> body)
    {
        body(Builder, Entity1, Entity2, Entity3, Entity4, Entity5);
        return Builder;
    }
}