using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(LockTestSuite))]
public interface ILockTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> SelectWithForShareData { get; }
    static abstract TheoryData<SqlTestCase> SelectWithForUpdateData { get; }
}