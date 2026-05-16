using SqlInterpol.Config;
using Xunit.Abstractions;

namespace SqlInterpol.Test.Models;

public sealed class SqlErrorTestCase : SqlTestCaseBase
{
    public Type ExpectedExceptionType { get; private set; } = default!;
    public string ExpectedMessageSubstring { get; private set; } = "";

    // Required for xUnit deserialization
    public SqlErrorTestCase() { }

    public SqlErrorTestCase(SqlDialectKind dialect, Type expectedExceptionType, string expectedMessageSubstring = "") 
        : base(dialect)
    {
        ExpectedExceptionType = expectedExceptionType ?? throw new ArgumentNullException(nameof(expectedExceptionType));
        ExpectedMessageSubstring = expectedMessageSubstring;
    }

    public override void Serialize(IXunitSerializationInfo info)
    {
        base.Serialize(info);
        info.AddValue(nameof(ExpectedExceptionType), ExpectedExceptionType.AssemblyQualifiedName);
        info.AddValue(nameof(ExpectedMessageSubstring), ExpectedMessageSubstring);
    }

    public override void Deserialize(IXunitSerializationInfo info)
    {
        base.Deserialize(info);
        var typeName = info.GetValue<string>(nameof(ExpectedExceptionType));
        ExpectedExceptionType = Type.GetType(typeName)!;
        ExpectedMessageSubstring = info.GetValue<string>(nameof(ExpectedMessageSubstring));
    }

    public override string ToString() => $"{Dialect.Value} -> Throws {ExpectedExceptionType.Name}";

    public void AssertException(Exception? exception)
    {
        Assert.NotNull(exception);
        
        // Use the non-generic overload of IsType!
        Assert.IsType(ExpectedExceptionType, exception);
        
        if (!string.IsNullOrEmpty(ExpectedMessageSubstring))
        {
            Assert.Contains(ExpectedMessageSubstring, exception.Message);
        }
    }
}