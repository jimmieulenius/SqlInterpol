namespace SqlInterpol;

public readonly record struct SqlEntityFluentBuilder<T1>(SqlBuilder Builder, ISqlEntity<T1> E1) : ISqlEntityFluentBuilder
{
    public SqlEntityFluentBuilder<T1, T2> Entity<T2>(string? name = null, string? schema = null)
        => new(Builder, E1, ((ISqlEntityRegistry)Builder).RegisterEntity<T2>(name, schema));
}

public readonly record struct SqlEntityFluentBuilder<T1, T2>(SqlBuilder Builder, ISqlEntity<T1> E1, ISqlEntity<T2> E2) : ISqlEntityFluentBuilder
{
    public SqlEntityFluentBuilder<T1, T2, T3> Entity<T3>(string? name = null, string? schema = null)
        => new(Builder, E1, E2, ((ISqlEntityRegistry)Builder).RegisterEntity<T3>(name, schema));
}

public readonly record struct SqlEntityFluentBuilder<T1, T2, T3>(SqlBuilder Builder, ISqlEntity<T1> E1, ISqlEntity<T2> E2, ISqlEntity<T3> E3) : ISqlEntityFluentBuilder
{
}