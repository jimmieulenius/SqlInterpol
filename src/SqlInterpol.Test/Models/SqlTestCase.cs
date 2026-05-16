using SqlInterpol.Config;
using Xunit.Abstractions;

namespace SqlInterpol.Test.Models;

public sealed class SqlTestCase : SqlTestCaseBase
{
    public string[] ExpectedSql { get; private set; } = [];

    // Required for xUnit deserialization
    public SqlTestCase() { }

    public SqlTestCase(SqlDialectKind dialect, string[] expectedSql) : base(dialect)
    {
        ExpectedSql = expectedSql ?? throw new ArgumentNullException(nameof(expectedSql));
        
        if (expectedSql.Length == 0)
            throw new ArgumentException("ExpectedSql cannot be empty.", nameof(expectedSql));
    }

    public override void Serialize(IXunitSerializationInfo info)
    {
        base.Serialize(info);
        info.AddValue(nameof(ExpectedSql), ExpectedSql);
    }

    public override void Deserialize(IXunitSerializationInfo info)
    {
        base.Deserialize(info);
        ExpectedSql = info.GetValue<string[]>(nameof(ExpectedSql));
    }

    public override string ToString() => Dialect.Value;

    public void AssertSql(string actualSql, int expectedIndex = 0)
    {
        const string WindowsLineEnding = "\r\n";
        const string UnixLineEnding = "\n";

        var expected = ExpectedSql[expectedIndex].Replace(WindowsLineEnding, UnixLineEnding);
        var actual = actualSql.Replace(WindowsLineEnding, UnixLineEnding);

        Assert.Equal(expected, actual);
    }
}