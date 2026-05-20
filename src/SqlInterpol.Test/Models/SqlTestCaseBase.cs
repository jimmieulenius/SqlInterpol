using SqlInterpol.Test.Helpers;
using Xunit.Abstractions;

namespace SqlInterpol.Test.Models;

public abstract class SqlTestCaseBase : IXunitSerializable
{
    public SqlDialectKind Dialect { get; protected set; } = default!;

    // Required for xUnit deserialization
    public SqlTestCaseBase() { }

    protected SqlTestCaseBase(SqlDialectKind dialect)
    {
        Dialect = dialect;
    }

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null)
    {
        return SqlBuilderFactory.Create(Dialect, options);
    }

    public virtual void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Dialect), Dialect.Value);
    }

    public virtual void Deserialize(IXunitSerializationInfo info)
    {
        Dialect = info.GetValue<string>(nameof(Dialect));
    }
}