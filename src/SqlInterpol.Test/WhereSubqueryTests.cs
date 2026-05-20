using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class WhereSubqueryTests
{
    [Theory]
    [MemberData(nameof(WhereInSubqueryData))]
    public void Where_In_Subquery(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        var activeCategoriesQuery = db
            .Entity<Product>(alias: "p")
            .Query(p => db.Append($$"""
                SELECT 
                    {{p[x => x.CategoryId]}}
                FROM {{p}}
                WHERE {{p[x => x.Price]}} > 0
                """));

        // Act
        var result = db.Entity<Category>(alias: "c").Query(c => db.Append($$"""
            SELECT 
                {{c[x => x.Name]}}
            FROM {{c}}
            WHERE {{c[x => x.Id]}} IN
            (
                {{activeCategoriesQuery}}
            )
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> WhereInSubqueryData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT 
                    <<c>>.<<Name>>
                FROM <<Category>> AS <<c>>
                WHERE <<c>>.<<Id>> IN
                (
                    SELECT 
                        <<p>>.<<CategoryId>>
                    FROM <<dbo>>.<<Products>> AS <<p>>
                    WHERE <<p>>.<<Price>> > 0
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT 
                    "c"."Name"
                FROM "Category" AS "c"
                WHERE "c"."Id" IN
                (
                    SELECT 
                        "p"."CategoryId"
                    FROM "dbo"."Products" AS "p"
                    WHERE "p"."Price" > 0
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT 
                    `c`.`Name`
                FROM `Category` AS `c`
                WHERE `c`.`Id` IN
                (
                    SELECT 
                        `p`.`CategoryId`
                    FROM `dbo`.`Products` AS `p`
                    WHERE `p`.`Price` > 0
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT 
                    "c"."Name"
                FROM "Category" AS "c"
                WHERE "c"."Id" IN
                (
                    SELECT 
                        "p"."CategoryId"
                    FROM "dbo"."Products" AS "p"
                    WHERE "p"."Price" > 0
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT 
                    "c"."Name"
                FROM "Category" AS "c"
                WHERE "c"."Id" IN
                (
                    SELECT 
                        "p"."CategoryId"
                    FROM "dbo"."Products" AS "p"
                    WHERE "p"."Price" > 0
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT 
                    "c"."Name"
                FROM "Category" AS "c"
                WHERE "c"."Id" IN
                (
                    SELECT 
                        "p"."CategoryId"
                    FROM "dbo"."Products" AS "p"
                    WHERE "p"."Price" > 0
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT 
                    [c].[Name]
                FROM [Category] AS [c]
                WHERE [c].[Id] IN
                (
                    SELECT 
                        [p].[CategoryId]
                    FROM [dbo].[Products] AS [p]
                    WHERE [p].[Price] > 0
                )
                """
            ]
        )
    ];
}