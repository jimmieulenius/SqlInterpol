using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ICrossDialectSqlTranspilationTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<bool, SqlTestCase> PagingToggleData { get; }
    static abstract TheoryData<bool, SqlTestCase> PagingHardcodedToggleData { get; }
    static abstract TheoryData<bool, SqlTestCase> RowLockingToggleData { get; }
    static abstract TheoryData<bool, SqlTestCase> SelectIntoToggleData { get; }
}