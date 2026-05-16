using SqlInterpol.Config;
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

        // Act - The developer always writes standard FOR UPDATE
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
    [MemberData(nameof(UnsupportedLockData))]
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
    [MemberData(nameof(UnsupportedLockData))]
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

    public static TheoryData<SqlTestCase> SelectWithForUpdateData =>
    [
        // MySQL moves it to the end (using backticks)
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
        // Oracle moves it to the end (using quotes & colon params)
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
        // PostgreSQL moves it to the end
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
        // SQLite doesn't support row-level locks! It completely (and safely) strips it out.
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Id" = ?0
                """
            ]
        ),
        // SQL Server translates it inline automatically, perfectly formatted!
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
        // MySQL supports FOR SHARE syntax at the end
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
        // Oracle does not natively support SELECT FOR SHARE. ORMs traditionally fallback to FOR UPDATE.
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
        // PostgreSQL moves to end
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
        // SQLite ignores it safely
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Id" = ?0
                """
            ]
        ),
        // SQL Server translates to ROWLOCK, HOLDLOCK
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

    public static TheoryData<SqlErrorTestCase> UnsupportedLockData =>
    [
        // Test that CustomDb throws if a user tries to use FOR UPDATE
        new SqlErrorTestCase(
            SqlDialectKind.CustomDb, 
            expectedExceptionType: typeof(NotSupportedException), 
            expectedMessageSubstring: "SqlLockFragment")
    ];
}