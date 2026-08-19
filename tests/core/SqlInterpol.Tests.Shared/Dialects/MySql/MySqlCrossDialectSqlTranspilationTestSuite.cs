using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlCrossDialectSqlTranspilationTestSuite : ICrossDialectSqlTranspilationTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<bool, SqlTestCase> PagingToggleData => new()
    {
        { 
            true, 
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT * FROM Products LIMIT @p0 OFFSET @p1
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
                    FOR UPDATE
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
                expectedSql: [
                    """
                    CREATE TABLE `#Temp` AS
                    SELECT Id 
                    FROM Products
                    """
                ]
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