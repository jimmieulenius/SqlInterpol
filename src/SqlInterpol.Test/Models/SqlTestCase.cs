using SqlInterpol.Config;
using SqlInterpol.Test.Helpers;
using Xunit.Abstractions;

namespace SqlInterpol.Test.Models;

public sealed class SqlTestCase : IXunitSerializable
{
    public SqlDialectKind Dialect { get; private set; } = default;
    public string[] ExpectedSql { get; private set; } = [];

    // Required parameterless constructor for xUnit deserialization
    public SqlTestCase() { }

    public SqlTestCase(SqlDialectKind dialect, string[] expectedSql)
    {
        Dialect = dialect;
        ExpectedSql = expectedSql?.Length > 0 ? expectedSql : throw new ArgumentException("Expected SQL cannot be empty", nameof(expectedSql));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Dialect), Dialect.Value);
        info.AddValue(nameof(ExpectedSql), ExpectedSql);
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Dialect = info.GetValue<string>(nameof(Dialect));
        ExpectedSql = info.GetValue<string[]>(nameof(ExpectedSql));
    }

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilderFactory.Create(Dialect, options);

    public override string ToString() => Dialect;
}