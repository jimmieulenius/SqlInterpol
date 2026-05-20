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
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
            INTO #TempProducts
            FROM {{p}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Empty(result.Parameters);
    }

    [Theory]
    [MemberData(nameof(SelectIntoParameterizedData))]
    public void Select_IntoNewTable_WithParameterizedTarget(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var target = Sql.Raw("#TempProducts");

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
            INTO {{target}}
            FROM {{p}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        
        // Target table strings must be consumed structurally as literals by RewriteSegments, 
        // leaving parameters empty.
        Assert.Empty(result.Parameters);
    }

    [Theory]
    [MemberData(nameof(UnsupportedSelectIntoData))]
    public void Select_Into_ThrowsException_ForUnsupportedDialect(SqlErrorTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var exception = Record.Exception(() => 
        {
            db.Query<Product>(p => db.Append($$"""
                SELECT {{p[x => x.Id]}}
                INTO #TempProducts
                FROM {{p}}
                """)).Build();
        });

        // Assert
        testCase.AssertException(exception);
    }

    // --- TEST DATA ---

    public static TheoryData<SqlTestCase> SelectIntoData =>
    [
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
                INTO "#TempProducts"
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
                INTO [#TempProducts]
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> SelectIntoParameterizedData =>
    [
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

    public static TheoryData<SqlErrorTestCase> UnsupportedSelectIntoData =>
    [
        new SqlErrorTestCase(
            SqlDialectKind.CustomDb, 
            typeof(SqlDialectException), 
            "'SELECT INTO' is not supported"
        ),
        new SqlErrorTestCase(
            SqlDialectKind.Firebird, 
            typeof(SqlDialectException), 
            "'SELECT INTO' is not supported"
        )
    ];
}