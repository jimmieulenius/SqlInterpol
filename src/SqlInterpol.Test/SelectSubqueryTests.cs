using System.Runtime.CompilerServices;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class SelectSubqueryTests
{
    [Theory]
    [MemberData(nameof(SelectSubqueryData))]
    public void Select_WithInlineSubquery(SqlTestCase testCase)
    {
        // Arrange
        var activeStatus = 100;
        var minPrice = 101;

        var db = testCase.CreateBuilder();
        
        // Act
        db.Entity<Product>(out var p);

        // 1. Define subquery inline. It securely captures the 'p' reference!
        var categorySubquery = db.Entity<Category>(out var c).Subquery(c, sub => 
            sub.Append($$"""
            SELECT
                {{c.Name}}
            FROM {{c}}
            WHERE {{c.Id}} = {{p.CategoryId}} AND {{c.IsActive}} = {{activeStatus}}
            """));

        // 2. Main query safely correlates the alias
        var result = db.Append($$"""
            SELECT 
                {{p.Id}},
                (
                    {{categorySubquery}}
                ) AS CategoryName
            FROM {{p}} AS prod
            WHERE {{p.Price}} > {{minPrice}}
            """).Build();

        // Assert
        testCase.AssertSql(result.Sql, 0); // Assert against the first expected string
        Assert.Equal(2, result.Parameters.Count);
    }

    [Theory]
    [MemberData(nameof(SelectSubqueryData))]
    public void Select_WithFunctionSubquery(SqlTestCase testCase)
    {
        // Arrange
        var activeStatus = 100;
        var minPrice = 101;

        // Act
        // First alias: 'prod'
        var db1 = testCase.CreateBuilder();     
        db1.Entity<Product>(out var p1);
        
        var result1 = db1.Append($$"""
            SELECT 
                {{p1.Id}},
                (
                    {{db1.BuildCategorySubquery(p1, activeStatus)}}
                ) AS CategoryName
            FROM {{p1}} AS prod
            WHERE {{p1.Price}} > {{minPrice}}
            """).Build();

        // Second alias: 'second_prod' - ensures no state bleed between queries
        var db2 = testCase.CreateBuilder();
        db2.Entity<Product>(out var p2);
        
        var result2 = db2.Append($$"""
            SELECT 
                {{p2.Id}},
                (
                    {{db2.BuildCategorySubquery(p2, activeStatus)}}
                ) AS CategoryName
            FROM {{p2}} AS second_prod
            WHERE {{p2.Price}} > {{minPrice}}
            """).Build();

        // Assert
        testCase.AssertSql(result1.Sql, 0); // Assert against the first expected string
        testCase.AssertSql(result2.Sql, 1); // Assert against the second expected string
    }

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
            SqlDialectKind.Firebird,
            [
                """
                SELECT 
                    prod."Id",
                    (
                        SELECT
                            "Category"."Name"
                        FROM "Category"
                        WHERE "Category"."Id" = prod."CategoryId" AND "Category"."IsActive" = @p0
                    ) AS CategoryName
                FROM "dbo"."Products" AS prod
                WHERE prod."Price" > @p1
                """,
                """
                SELECT 
                    second_prod."Id",
                    (
                        SELECT
                            "Category"."Name"
                        FROM "Category"
                        WHERE "Category"."Id" = second_prod."CategoryId" AND "Category"."IsActive" = @p0
                    ) AS CategoryName
                FROM "dbo"."Products" AS second_prod
                WHERE second_prod."Price" > @p1
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
                        WHERE "Category"."Id" = prod."CategoryId" AND "Category"."IsActive" = @p1
                    ) AS CategoryName
                FROM "dbo"."Products" AS prod
                WHERE prod."Price" > @p2
                """,
                """
                SELECT 
                    second_prod."Id",
                    (
                        SELECT
                            "Category"."Name"
                        FROM "Category"
                        WHERE "Category"."Id" = second_prod."CategoryId" AND "Category"."IsActive" = @p1
                    ) AS CategoryName
                FROM "dbo"."Products" AS second_prod
                WHERE second_prod."Price" > @p2
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

internal static class SelectSubqueryTestExtensions
{
    public static ISqlQuery<Category> BuildCategorySubquery(this SqlBuilder db, Product pOuter, int activeStatus) => 
        db.Entity(pOuter, out var p) // Imports the outer entity!
          .Entity<Category>(out var c) // Registers the inner entity!
          .Subquery(c, sub => sub.Append($$"""
            SELECT
                {{c.Name}}
            FROM {{c}}
            WHERE {{c.Id}} = {{p.CategoryId}} AND {{c.IsActive}} = {{activeStatus}}
            """));
}