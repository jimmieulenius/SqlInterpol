using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleCrossDialectSqlTranspilationTestSuite : ICrossDialectSqlTranspilationTestSuite
{
    private static readonly object[] _expectedParameters = [10, 20];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<bool, SqlTestCase> PagingToggleData => new()
    {
        { 
            true, 
            new SqlTestCase(
                expectedSql: [
                    """
                    SELECT * FROM Products OFFSET :1 ROWS FETCH NEXT :0 ROWS ONLY
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
                    SELECT * FROM Products LIMIT :0 OFFSET :1
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
                    SELECT * FROM Products OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
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
                    CREATE TABLE "#Temp" AS
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