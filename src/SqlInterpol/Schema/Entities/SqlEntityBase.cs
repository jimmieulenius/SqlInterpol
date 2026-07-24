using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Schema;

/// <summary>
/// Provides a base implementation for SQL entities, managing reference mapping and declarations.
/// </summary>
public abstract class SqlEntityBase : ISqlEntityBase
{
    /// <inheritdoc />
    public SqlEntityRole Role { get; set; } = SqlEntityRole.Table;

    /// <inheritdoc />
    public abstract Type ModelType { get; }

    /// <inheritdoc />
    public ISqlReference Reference { get; protected set; } = null!;

    /// <inheritdoc />
    public ISqlDeclaration Declaration { get; protected set; } = null!;

    /// <inheritdoc />
    public abstract string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default);
}

/// <summary>
/// Provides a strongly-typed base implementation for SQL entities bound to a specific CLR model.
/// </summary>
/// <typeparam name="T">The CLR model type.</typeparam>
public abstract class SqlEntityBase<T> : SqlEntityBase, ISqlEntityBase<T>
{
    /// <inheritdoc />
    public override Type ModelType => typeof(T);
}