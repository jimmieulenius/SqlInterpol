using SqlInterpol.Configuration;
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

        // Act
        testCase.Action(() => 
        {
            // 1. Declare Product and build the subquery fragment
            db.Entity<Product>(out var p);
            var activeCategoriesQuery = db.Fragment(f => f.Append($$"""
                SELECT 
                    {{p.CategoryId}}
                FROM {{p}} AS p
                WHERE {{p.Price}} > 0
                """));

            // 2. Declare Category and build the main query, injecting the fragment natively
            db.Entity<Category>(out var c);
            return db.Append($$"""
                SELECT 
                    {{c.Name}}
                FROM {{c}} AS c
                WHERE {{c.Id}} IN
                (
                    {{activeCategoriesQuery}}
                )
                """).Build();
        });

        // Assert
        testCase.Assert();
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
                FROM "Category" "c"
                WHERE "c"."Id" IN
                (
                    SELECT 
                        "p"."CategoryId"
                    FROM "dbo"."Products" "p"
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