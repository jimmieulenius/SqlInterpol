using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class SelectIntoTests
{
    [Theory]
    [MemberData(nameof(SelectIntoData))]
    public void Select_IntoNewTable(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}, {{p.Name}}
                INTO #TempProducts
                FROM {{p}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [Theory]
    [MemberData(nameof(SelectIntoParameterizedData))]
    public void Select_IntoNewTable_WithParameterizedTarget(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var target = Sql.Raw("#TempProducts");

        // Act
        testCase.Action(() => 
        {
            db.Entity<Product>(out var p);

#pragma warning disable SQLIG10
            return db.Append($$"""
                SELECT {{p.Id}}, {{p.Name}}
                INTO {{target}}
                FROM {{p}}
                """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> SelectIntoData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            typeof(SqlDialectException), 
            "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'SELECT INTO'."
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird, 
            typeof(SqlDialectException), 
            "The SQL dialect 'Firebird' does not support the operation or fragment type: 'SELECT INTO'."
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                CREATE TABLE `#TempProducts` AS
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                CREATE TABLE "#TempProducts" AS
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                INTO #TempProducts
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                CREATE TABLE "#TempProducts" AS
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                INTO #TempProducts
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> SelectIntoParameterizedData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            typeof(SqlDialectException), 
            "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'SELECT INTO'."
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird, 
            typeof(SqlDialectException), 
            "The SQL dialect 'Firebird' does not support the operation or fragment type: 'SELECT INTO'."
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                CREATE TABLE #TempProducts AS
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                CREATE TABLE #TempProducts AS
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                INTO #TempProducts
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                CREATE TABLE #TempProducts AS
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                INTO #TempProducts
                FROM [dbo].[Products]
                """
            ]
        )
    ];
}