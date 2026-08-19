using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdCrossDialectSqlTranspilationTestSuite : ICrossDialectSqlTranspilationTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<bool, SqlTestCase> PagingToggleData => new()
    {
        { 
            true, 
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT * FROM Products FIRST @p0 SKIP @p1
                    """
                ], 
                expectedParameters: _expectedParameters
            ) 
        },
        { 
            false, 
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT * FROM Products LIMIT @p0 OFFSET @p1
                    """
                ], 
                expectedParameters: _expectedParameters
            ) 
        }
    };

    public static TheoryData<bool, SqlTestCase> PagingHardcodedToggleData => new()
    {
        { 
            true, 
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT * FROM Products LIMIT 10 OFFSET 20
                    """
                ]
            ) 
        },
        { 
            false, 
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT * FROM Products LIMIT 10 OFFSET 20
                    """
                ]
            ) 
        }
    };

    public static TheoryData<bool, SqlTestCase> RowLockingToggleData => new()
    {
        { 
            true, 
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT * FROM Products
                    WITH LOCK
                    """
                ]
            ) 
        },
        { 
            false, 
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT * FROM Products FOR UPDATE
                    """
                ]
            ) 
        }
    };

    public static TheoryData<bool, SqlTestCase> SelectIntoToggleData => new()
    {
        { 
            true, 
            new SqlTestCase(
                expectedExceptionType: typeof(SqlDialectException), 
                expectedExceptionMessage: "The SQL dialect 'Firebird' does not support the operation or fragment type: 'SELECT INTO'."
            ) 
        },
        { 
            false, 
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT Id INTO #Temp FROM Products
                    """
                ]
            ) 
        }
    };
}