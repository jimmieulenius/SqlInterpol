using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class SelectSubqueryTests
{
    [Theory]
    [MemberData(nameof(SelectSubqueryData))]
    public void Select_WithInlineSubquery(SqlTestCase testCase)
    {
        // Arrange
        var activeStatus = true;
        var minPrice = 100;

        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Entity<Product>().Query(p =>
        {
            // 1. Define subquery inline. It securely captures the 'p' reference!
            var categorySubquery = db.Entity<Category>().Query(c => 
                db.Append($$"""
                SELECT
                    {{c[x => x.Name]}}
                FROM {{c}}
                WHERE {{c[x => x.Id]}} = {{p[x => x.CategoryId]}} AND {{c[x => x.IsActive]}} = {{activeStatus}}
                """));

            // 2. Main query safely correlates the alias
            db.Append($$"""
            SELECT 
                {{p[x => x.Id]}},
                (
                    {{categorySubquery}}
                ) AS CategoryName
            FROM {{p}} AS prod
            WHERE {{p[x => x.Price]}} > {{minPrice}}
            """);
        }).Build();

        // Assert
        testCase.AssertSql(result.Sql, 0); // Assert against the first expected string
        Assert.Equal(2, result.Parameters.Count);
    }

    [Theory]
    [MemberData(nameof(SelectSubqueryData))]
    public void Select_WithFunctionSubquery(SqlTestCase testCase)
    {
        // Arrange
        var activeStatus = true;
        var minPrice = 100;

        // Act
        // First alias: 'prod'
        var db1 = testCase.CreateBuilder();     
        var result1 = db1.Entity<Product>().Query(p =>
        {
            db1.Append($$"""
            SELECT 
                {{p[x => x.Id]}},
                (
                    {{BuildCategorySubquery(db1, p, activeStatus)}}
                ) AS CategoryName
            FROM {{p}} AS prod
            WHERE {{p[x => x.Price]}} > {{minPrice}}
            """);
        }).Build();

        // Second alias: 'second_prod' - ensures no state bleed between queries
        var db2 = testCase.CreateBuilder();
        var result2 = db2.Entity<Product>().Query(p =>
        {
            db2.Append($$"""
            SELECT 
                {{p[x => x.Id]}},
                (
                    {{BuildCategorySubquery(db2, p, activeStatus)}}
                ) AS CategoryName
            FROM {{p}} AS second_prod
            WHERE {{p[x => x.Price]}} > {{minPrice}}
            """);
        }).Build();

        // Assert
        testCase.AssertSql(result1.Sql, 0); // Assert against the first expected string
        testCase.AssertSql(result2.Sql, 1); // Assert against the second expected string
    }

    // Helper method used by the function test
    private ISqlQuery BuildCategorySubquery(SqlBuilder db, ISqlEntity<Product> p, bool activeStatus) => 
        db.Entity<Category>().Query(c => 
            db.Append($$"""
            SELECT
                {{c[x => x.Name]}}
            FROM {{c}}
            WHERE {{c[x => x.Id]}} = {{p[x => x.CategoryId]}} AND {{c[x => x.IsActive]}} = {{activeStatus}}
            """));

    public static TheoryData<SqlTestCase> SelectSubqueryData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT 
                    prod.<<Id>>,
                    (
                        SELECT
                            <<Category>>.<<Name>>
                        FROM <<Category>>
                        WHERE <<Category>>.<<Id>> = prod.<<CategoryId>> AND <<Category>>.<<IsActive>> = !!100
                    ) AS CategoryName
                FROM <<dbo>>.<<Products>> AS prod
                WHERE prod.<<Price>> > !!101
                """,
                """
                SELECT 
                    second_prod.<<Id>>,
                    (
                        SELECT
                            <<Category>>.<<Name>>
                        FROM <<Category>>
                        WHERE <<Category>>.<<Id>> = second_prod.<<CategoryId>> AND <<Category>>.<<IsActive>> = !!100
                    ) AS CategoryName
                FROM <<dbo>>.<<Products>> AS second_prod
                WHERE second_prod.<<Price>> > !!101
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT 
                    prod.`Id`,
                    (
                        SELECT
                            `Category`.`Name`
                        FROM `Category`
                        WHERE `Category`.`Id` = prod.`CategoryId` AND `Category`.`IsActive` = @p0
                    ) AS CategoryName
                FROM `dbo`.`Products` AS prod
                WHERE prod.`Price` > @p1
                """,
                """
                SELECT 
                    second_prod.`Id`,
                    (
                        SELECT
                            `Category`.`Name`
                        FROM `Category`
                        WHERE `Category`.`Id` = second_prod.`CategoryId` AND `Category`.`IsActive` = @p0
                    ) AS CategoryName
                FROM `dbo`.`Products` AS second_prod
                WHERE second_prod.`Price` > @p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT 
                    prod."Id",
                    (
                        SELECT
                            "Category"."Name"
                        FROM "Category"
                        WHERE "Category"."Id" = prod."CategoryId" AND "Category"."IsActive" = :0
                    ) AS CategoryName
                FROM "dbo"."Products" AS prod
                WHERE prod."Price" > :1
                """,
                """
                SELECT 
                    second_prod."Id",
                    (
                        SELECT
                            "Category"."Name"
                        FROM "Category"
                        WHERE "Category"."Id" = second_prod."CategoryId" AND "Category"."IsActive" = :0
                    ) AS CategoryName
                FROM "dbo"."Products" AS second_prod
                WHERE second_prod."Price" > :1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT 
                    prod."Id",
                    (
                        SELECT
                            "Category"."Name"
                        FROM "Category"
                        WHERE "Category"."Id" = prod."CategoryId" AND "Category"."IsActive" = $1
                    ) AS CategoryName
                FROM "dbo"."Products" AS prod
                WHERE prod."Price" > $2
                """,
                """
                SELECT 
                    second_prod."Id",
                    (
                        SELECT
                            "Category"."Name"
                        FROM "Category"
                        WHERE "Category"."Id" = second_prod."CategoryId" AND "Category"."IsActive" = $1
                    ) AS CategoryName
                FROM "dbo"."Products" AS second_prod
                WHERE second_prod."Price" > $2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT 
                    prod."Id",
                    (
                        SELECT
                            "Category"."Name"
                        FROM "Category"
                        WHERE "Category"."Id" = prod."CategoryId" AND "Category"."IsActive" = ?0
                    ) AS CategoryName
                FROM "dbo"."Products" AS prod
                WHERE prod."Price" > ?1
                """,
                """
                SELECT 
                    second_prod."Id",
                    (
                        SELECT
                            "Category"."Name"
                        FROM "Category"
                        WHERE "Category"."Id" = second_prod."CategoryId" AND "Category"."IsActive" = ?0
                    ) AS CategoryName
                FROM "dbo"."Products" AS second_prod
                WHERE second_prod."Price" > ?1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT 
                    prod.[Id],
                    (
                        SELECT
                            [Category].[Name]
                        FROM [Category]
                        WHERE [Category].[Id] = prod.[CategoryId] AND [Category].[IsActive] = @p0
                    ) AS CategoryName
                FROM [dbo].[Products] AS prod
                WHERE prod.[Price] > @p1
                """,
                """
                SELECT 
                    second_prod.[Id],
                    (
                        SELECT
                            [Category].[Name]
                        FROM [Category]
                        WHERE [Category].[Id] = second_prod.[CategoryId] AND [Category].[IsActive] = @p0
                    ) AS CategoryName
                FROM [dbo].[Products] AS second_prod
                WHERE second_prod.[Price] > @p1
                """
            ]
        )
    ];
}