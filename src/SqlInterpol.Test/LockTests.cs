using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class LockTests
{
    [Theory]
    [MemberData(nameof(SelectWithForUpdateData))]
    public void Select_WithForUpdate(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int id = 5;

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
            FROM {{p}} FOR UPDATE
            WHERE {{p[x => x.Id]}} = {{id}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        
        Assert.Single(result.Parameters);
        Assert.Equal(5, result.Parameters.First().Value);
    }

    [Theory]
    [MemberData(nameof(SelectWithForShareData))]
    public void Select_WithForShare(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int id = 5;

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
            FROM {{p}} FOR SHARE
            WHERE {{p[x => x.Id]}} = {{id}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(UnsupportedForUpdateData))]
    public void Select_WithForUpdate_ThrowsException_ForUnsupportedDialect(SqlErrorTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int id = 5;

        // Act
        var exception = Record.Exception(() => 
        {
            db.Query<Product>(p => db.Append($$"""
                SELECT {{p[x => x.Id]}}
                FROM {{p}} FOR UPDATE
                WHERE {{p[x => x.Id]}} = {{id}}
                """)).Build();
        });

        // Assert
        testCase.AssertException(exception);
    }

    [Theory]
    [MemberData(nameof(UnsupportedForShareData))]
    public void Select_WithForShare_ThrowsException_ForUnsupportedDialect(SqlErrorTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int id = 5;

        // Act
        var exception = Record.Exception(() => 
        {
            db.Query<Product>(p => db.Append($$"""
                SELECT {{p[x => x.Id]}}
                FROM {{p}} FOR SHARE
                WHERE {{p[x => x.Id]}} = {{id}}
                """)).Build();
        });

        // Assert
        testCase.AssertException(exception);
    }

    // --- TEST DATA ---

    public static TheoryData<SqlTestCase> SelectWithForUpdateData =>
    [
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Id" = @p0
                WITH LOCK
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`Id` = @p0
                FOR UPDATE
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Id" = :0
                FOR UPDATE
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Id" = $1
                FOR UPDATE
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products] WITH (UPDLOCK)
                WHERE [dbo].[Products].[Id] = @p0
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> SelectWithForShareData =>
    [
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`Id` = @p0
                FOR SHARE
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Id" = $1
                FOR SHARE
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products] WITH (ROWLOCK, HOLDLOCK)
                WHERE [dbo].[Products].[Id] = @p0
                """
            ]
        )
    ];

    public static TheoryData<SqlErrorTestCase> UnsupportedForUpdateData =>
    [
        new SqlErrorTestCase(
            SqlDialectKind.CustomDb,
            typeof(SqlDialectException),
            "'FOR UPDATE' is not supported"
        ),
        new SqlErrorTestCase(
            SqlDialectKind.SqLite,
            typeof(SqlDialectException),
            "'FOR UPDATE' is not supported"
        )
    ];

    public static TheoryData<SqlErrorTestCase> UnsupportedForShareData =>
    [
        new SqlErrorTestCase(
            SqlDialectKind.CustomDb,
            typeof(SqlDialectException),
            "'FOR SHARE' is not supported"
        ),
        new SqlErrorTestCase(
            SqlDialectKind.Firebird,
            typeof(SqlDialectException),
            "'FOR SHARE' is not supported"
        ),
        new SqlErrorTestCase(
            SqlDialectKind.Oracle,
            typeof(SqlDialectException),
            "'FOR SHARE' is not supported"
        ),
        new SqlErrorTestCase(
            SqlDialectKind.SqLite,
            typeof(SqlDialectException),
            "'FOR SHARE' is not supported"
        )
    ];
}