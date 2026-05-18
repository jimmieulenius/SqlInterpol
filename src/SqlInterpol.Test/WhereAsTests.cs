using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class WhereAsTests
{
    [Theory]
    [MemberData(nameof(WhereWithAliasedEntityData))]
    public void Where_WithAliasedEntity_ShouldUseAliasPrefix(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int categoryId = 5;

        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}} AS p
            WHERE {{p[x => x.CategoryId]}} = {{categoryId}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> WhereWithAliasedEntityData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT
                    p.<<Id>>
                FROM <<dbo>>.<<Products>> AS p
                WHERE p.<<CategoryId>> = !!100
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
                    p.`Id`
                FROM `dbo`.`Products` AS p
                WHERE p.`CategoryId` = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    p."Id"
                FROM "dbo"."Products" AS p
                WHERE p."CategoryId" = :0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    p."Id"
                FROM "dbo"."Products" AS p
                WHERE p."CategoryId" = $1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            [
                """
                SELECT
                    p."Id"
                FROM "dbo"."Products" AS p
                WHERE p."CategoryId" = ?0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            [
                """
                SELECT
                    p.[Id]
                FROM [dbo].[Products] AS p
                WHERE p.[CategoryId] = @p0
                """
            ]
        )
    ];
}