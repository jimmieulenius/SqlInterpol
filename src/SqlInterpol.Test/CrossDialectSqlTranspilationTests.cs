using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class CrossDialectSqlTranspilationTests
{
    [Theory]
    [MemberData(nameof(PagingToggleData))]
    public void Paging_Toggle(bool xvSql, SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int limit = 10;
        int offset = 20;
        
        // Act
        testCase.Action(() => 
        {
            db.Context.Options.CrossVendorSqlTranspilation = xvSql;
            return [db.Append($"SELECT * FROM Products LIMIT {limit} OFFSET {offset}").Build()];
        });

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(PagingHardcodedToggleData))]
    public void Paging_Hardcoded_Toggle(bool xvSql, SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => 
        {
            db.Context.Options.CrossVendorSqlTranspilation = xvSql;
            return [db.Append($"SELECT * FROM Products LIMIT 10 OFFSET 20").Build()];
        });

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(RowLockingToggleData))]
    public void RowLocking_Toggle(bool xvSql, SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => 
        {
            db.Context.Options.CrossVendorSqlTranspilation = xvSql;
            return [db.Append($"SELECT * FROM Products FOR UPDATE").Build()];
        });

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(SelectIntoToggleData))]
    public void SelectInto_Toggle(bool xvSql, SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => 
        {
            db.Context.Options.CrossVendorSqlTranspilation = xvSql;
            return [db.Append($"SELECT Id INTO #Temp FROM Products").Build()];
        });

        // Assert
        testCase.Assert();
    }

    public static TheoryData<bool, SqlTestCase> PagingToggleData
    {
        get
        {
            object?[] expectedParams = [10, 20];

            return new TheoryData<bool, SqlTestCase>
            {
                // ====================================================================
                // 1. META-SQL ENABLED (Transpilation ON)
                // ====================================================================
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.CustomDb, 
                        [
                            """
                            SELECT * FROM Products LIMIT !!100 OFFSET !!101
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.MySql, 
                        [
                            """
                            SELECT * FROM Products LIMIT @p0 OFFSET @p1
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.Oracle, 
                        [
                            """
                            SELECT * FROM Products OFFSET :1 ROWS FETCH NEXT :0 ROWS ONLY
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.PostgreSql, 
                        [
                            """
                            SELECT * FROM Products LIMIT $1 OFFSET $2
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.SqLite, 
                        [
                            """
                            SELECT * FROM Products LIMIT @p1 OFFSET @p2
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.SqlServer, 
                        [
                            """
                            SELECT * FROM Products OFFSET @p1 ROWS FETCH NEXT @p0 ROWS ONLY
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },

                // ====================================================================
                // 2. META-SQL DISABLED (Raw Pass-Through)
                // ====================================================================
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.CustomDb, 
                        [
                            """
                            SELECT * FROM Products LIMIT !!100 OFFSET !!101
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.MySql, 
                        [
                            """
                            SELECT * FROM Products LIMIT @p0 OFFSET @p1
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.Oracle, 
                        [
                            """
                            SELECT * FROM Products LIMIT :0 OFFSET :1
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.PostgreSql, 
                        [
                            """
                            SELECT * FROM Products LIMIT $1 OFFSET $2
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.SqLite, 
                        [
                            """
                            SELECT * FROM Products LIMIT @p1 OFFSET @p2
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.SqlServer, 
                        [
                            """
                            SELECT * FROM Products LIMIT @p0 OFFSET @p1
                            """
                        ], 
                        expectedParameters: expectedParams
                    )
                }
            };
        }
    }

    public static TheoryData<bool, SqlTestCase> PagingHardcodedToggleData
    {
        get
        {
            string rawSql = "SELECT * FROM Products LIMIT 10 OFFSET 20";

            return new TheoryData<bool, SqlTestCase>
            {
                // ====================================================================
                // 1. META-SQL ENABLED (Transpilation ON)
                // ====================================================================
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.CustomDb, 
                        [rawSql]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.MySql, 
                        [rawSql]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.Oracle, 
                        [
                            """
                            SELECT * FROM Products OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
                            """
                        ]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.PostgreSql, 
                        [rawSql]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.SqLite, 
                        [rawSql]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.SqlServer, 
                        [
                            """
                            SELECT * FROM Products OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
                            """
                        ]
                    )
                },

                // ====================================================================
                // 2. META-SQL DISABLED (Raw Pass-Through)
                // ====================================================================
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.CustomDb, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.MySql, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.Oracle, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.PostgreSql, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.SqLite, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.SqlServer, 
                        [rawSql]
                    )
                }
            };
        }
    }

    public static TheoryData<bool, SqlTestCase> RowLockingToggleData
    {
        get
        {
            string rawSql = "SELECT * FROM Products FOR UPDATE";
            string transpiledSql = "SELECT * FROM Products\nFOR UPDATE";

            return new TheoryData<bool, SqlTestCase>
            {
                // --- META-SQL ENABLED (Transpilation ON) ---
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.CustomDb, 
                        typeof(SqlDialectException), 
                        "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'FOR UPDATE'."
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.MySql, 
                        [transpiledSql]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.Oracle, 
                        [transpiledSql]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.PostgreSql, 
                        [transpiledSql]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.SqLite, 
                        typeof(SqlDialectException), 
                        "The SQL dialect 'SqLite' does not support the operation or fragment type: 'FOR UPDATE'."
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.SqlServer, 
                        [
                            """
                            SELECT * FROM Products WITH (UPDLOCK)
                            """
                        ]
                    )
                },

                // --- META-SQL DISABLED (Raw Pass-Through) ---
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.CustomDb, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.MySql, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.Oracle, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.PostgreSql, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.SqLite, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.SqlServer, 
                        [rawSql]
                    )
                }
            };
        }
    }

    public static TheoryData<bool, SqlTestCase> SelectIntoToggleData
    {
        get
        {
            string rawSql = "SELECT Id INTO #Temp FROM Products";
            string createTableSql = "CREATE TABLE \"#Temp\" AS\nSELECT Id \nFROM Products";

            return new TheoryData<bool, SqlTestCase>
            {
                // --- META-SQL ENABLED (Transpilation ON) ---
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.CustomDb, 
                        typeof(SqlDialectException), 
                        "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'SELECT INTO'."
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.Firebird, 
                        typeof(SqlDialectException), 
                        "The SQL dialect 'Firebird' does not support the operation or fragment type: 'SELECT INTO'."
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.MySql, 
                        [
                            """
                            CREATE TABLE `#Temp` AS
                            SELECT Id 
                            FROM Products
                            """
                        ]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.Oracle, 
                        [createTableSql]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.PostgreSql, 
                        [rawSql]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.SqLite, 
                        [createTableSql]
                    )
                },
                {
                    true, 
                    new SqlTestCase(
                        SqlDialectKind.SqlServer, 
                        [rawSql]
                    )
                },

                // --- META-SQL DISABLED (Raw Pass-Through) ---
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.CustomDb, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.Firebird, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.MySql, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.Oracle, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.PostgreSql, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.SqLite, 
                        [rawSql]
                    )
                },
                {
                    false, 
                    new SqlTestCase(
                        SqlDialectKind.SqlServer, 
                        [rawSql]
                    )
                }
            };
        }
    }
}