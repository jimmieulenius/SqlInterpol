using SqlInterpol.Configuration;
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
        var activeStatus = 100;
        var minPrice = 101;

        var db1 = testCase.CreateBuilder();
        var db2 = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => 
        {
            // Scenario 1: 'prod' table alias
            db1.Entity<Product>(out var p1)
               .Entity<Category>(out var c1)
               // FIX: Use the 'out var' parameter to capture the actual ISqlQuery AST node!
               .Query(c1, out var categorySubquery1, () => db1.Append($$"""
                    SELECT
                        {{c1.Name}}
                    FROM {{c1}}
                    WHERE {{c1.Id}} = {{p1.CategoryId}} AND {{c1.IsActive}} = {{activeStatus}}
                    """));

            var result1 = db1.Append($$"""
                SELECT 
                    {{p1.Id}},
                    (
                        {{categorySubquery1}}
                    ) AS CategoryName
                FROM {{p1}} AS prod
                WHERE {{p1.Price}} > {{minPrice}}
                """).Build();

            // Scenario 2: 'second_prod' table alias (Guarantees no cross-statement alias leakage)
            db2.Entity<Product>(out var p2)
               .Entity<Category>(out var c2)
               // FIX: Same here, capture the query explicitly
               .Query(c2, out var categorySubquery2, () => db2.Append($$"""
                    SELECT
                        {{c2.Name}}
                    FROM {{c2}}
                    WHERE {{c2.Id}} = {{p2.CategoryId}} AND {{c2.IsActive}} = {{activeStatus}}
                    """));

            var result2 = db2.Append($$"""
                SELECT 
                    {{p2.Id}},
                    (
                        {{categorySubquery2}}
                    ) AS CategoryName
                FROM {{p2}} AS second_prod
                WHERE {{p2.Price}} > {{minPrice}}
                """).Build();

            return [result1, result2];
        });

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(SelectSubqueryData))]
    public void Select_WithFunctionSubquery(SqlTestCase testCase)
    {
        // Arrange
        var activeStatus = 100;
        var minPrice = 101;

        var db1 = testCase.CreateBuilder();     
        db1.Entity<Product>(out var p1);
        
        var db2 = testCase.CreateBuilder();
        db2.Entity<Product>(out var p2);

        // Act
        testCase.Action(() =>
        {
            return [
                db1.Append($$"""
                    SELECT 
                        {{p1.Id}},
                        (
                            {{db1.BuildCategorySubquery(p1, activeStatus)}}
                        ) AS CategoryName
                    FROM {{p1}} AS prod
                    WHERE {{p1.Price}} > {{minPrice}}
                    """).Build(),

                db2.Append($$"""
                    SELECT 
                        {{p2.Id}},
                        (
                            {{db2.BuildCategorySubquery(p2, activeStatus)}}
                        ) AS CategoryName
                    FROM {{p2}} AS second_prod
                    WHERE {{p2.Price}} > {{minPrice}}
                    """).Build()
            ];
        });

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> SelectSubqueryData
    {
        get
        {
            object?[] expectedParams = [100, 101];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb, 
                    [
                        """
                        SELECT 
                            <<prod>>.<<Id>>,
                            (
                                SELECT
                                    <<Category>>.<<Name>>
                                FROM <<Category>>
                                WHERE <<Category>>.<<Id>> = <<prod>>.<<CategoryId>> AND <<Category>>.<<IsActive>> = !!100
                            ) AS <<CategoryName>>
                        FROM <<dbo>>.<<Products>> AS <<prod>>
                        WHERE <<prod>>.<<Price>> > !!101
                        """,
                        """
                        SELECT 
                            <<second_prod>>.<<Id>>,
                            (
                                SELECT
                                    <<Category>>.<<Name>>
                                FROM <<Category>>
                                WHERE <<Category>>.<<Id>> = <<second_prod>>.<<CategoryId>> AND <<Category>>.<<IsActive>> = !!100
                            ) AS <<CategoryName>>
                        FROM <<dbo>>.<<Products>> AS <<second_prod>>
                        WHERE <<second_prod>>.<<Price>> > !!101
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        SELECT 
                            "prod"."Id",
                            (
                                SELECT
                                    "Category"."Name"
                                FROM "Category"
                                WHERE "Category"."Id" = "prod"."CategoryId" AND "Category"."IsActive" = @p0
                            ) AS "CategoryName"
                        FROM "dbo"."Products" AS "prod"
                        WHERE "prod"."Price" > @p1
                        """,
                        """
                        SELECT 
                            "second_prod"."Id",
                            (
                                SELECT
                                    "Category"."Name"
                                FROM "Category"
                                WHERE "Category"."Id" = "second_prod"."CategoryId" AND "Category"."IsActive" = @p0
                            ) AS "CategoryName"
                        FROM "dbo"."Products" AS "second_prod"
                        WHERE "second_prod"."Price" > @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT 
                            `prod`.`Id`,
                            (
                                SELECT
                                    `Category`.`Name`
                                FROM `Category`
                                WHERE `Category`.`Id` = `prod`.`CategoryId` AND `Category`.`IsActive` = @p0
                            ) AS `CategoryName`
                        FROM `dbo`.`Products` AS `prod`
                        WHERE `prod`.`Price` > @p1
                        """,
                        """
                        SELECT 
                            `second_prod`.`Id`,
                            (
                                SELECT
                                    `Category`.`Name`
                                FROM `Category`
                                WHERE `Category`.`Id` = `second_prod`.`CategoryId` AND `Category`.`IsActive` = @p0
                            ) AS `CategoryName`
                        FROM `dbo`.`Products` AS `second_prod`
                        WHERE `second_prod`.`Price` > @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle, 
                    [
                        """
                        SELECT 
                            "prod"."Id",
                            (
                                SELECT
                                    "Category"."Name"
                                FROM "Category"
                                WHERE "Category"."Id" = "prod"."CategoryId" AND "Category"."IsActive" = :0
                            ) AS "CategoryName"
                        FROM "dbo"."Products" "prod"
                        WHERE "prod"."Price" > :1
                        """,
                        """
                        SELECT 
                            "second_prod"."Id",
                            (
                                SELECT
                                    "Category"."Name"
                                FROM "Category"
                                WHERE "Category"."Id" = "second_prod"."CategoryId" AND "Category"."IsActive" = :0
                            ) AS "CategoryName"
                        FROM "dbo"."Products" "second_prod"
                        WHERE "second_prod"."Price" > :1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql, 
                    [
                        """
                        SELECT 
                            "prod"."Id",
                            (
                                SELECT
                                    "Category"."Name"
                                FROM "Category"
                                WHERE "Category"."Id" = "prod"."CategoryId" AND "Category"."IsActive" = $1
                            ) AS "CategoryName"
                        FROM "dbo"."Products" AS "prod"
                        WHERE "prod"."Price" > $2
                        """,
                        """
                        SELECT 
                            "second_prod"."Id",
                            (
                                SELECT
                                    "Category"."Name"
                                FROM "Category"
                                WHERE "Category"."Id" = "second_prod"."CategoryId" AND "Category"."IsActive" = $1
                            ) AS "CategoryName"
                        FROM "dbo"."Products" AS "second_prod"
                        WHERE "second_prod"."Price" > $2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        SELECT 
                            "prod"."Id",
                            (
                                SELECT
                                    "Category"."Name"
                                FROM "Category"
                                WHERE "Category"."Id" = "prod"."CategoryId" AND "Category"."IsActive" = @p1
                            ) AS "CategoryName"
                        FROM "dbo"."Products" AS "prod"
                        WHERE "prod"."Price" > @p2
                        """,
                        """
                        SELECT 
                            "second_prod"."Id",
                            (
                                SELECT
                                    "Category"."Name"
                                FROM "Category"
                                WHERE "Category"."Id" = "second_prod"."CategoryId" AND "Category"."IsActive" = @p1
                            ) AS "CategoryName"
                        FROM "dbo"."Products" AS "second_prod"
                        WHERE "second_prod"."Price" > @p2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT 
                            [prod].[Id],
                            (
                                SELECT
                                    [Category].[Name]
                                FROM [Category]
                                WHERE [Category].[Id] = [prod].[CategoryId] AND [Category].[IsActive] = @p0
                            ) AS [CategoryName]
                        FROM [dbo].[Products] AS [prod]
                        WHERE [prod].[Price] > @p1
                        """,
                        """
                        SELECT 
                            [second_prod].[Id],
                            (
                                SELECT
                                    [Category].[Name]
                                FROM [Category]
                                WHERE [Category].[Id] = [second_prod].[CategoryId] AND [Category].[IsActive] = @p0
                            ) AS [CategoryName]
                        FROM [dbo].[Products] AS [second_prod]
                        WHERE [second_prod].[Price] > @p1
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}

internal static partial class SelectSubqueryTestHelper
{
    [SqlQuery]
    internal static ISqlQuery<Category> BuildCategorySubquery(
        SqlBuilder db,
        Product p,
        int activeStatus)
    {
        return db
            .Entity<Category>(out var c)
            .Query(c, () => db.Append($$"""
                SELECT
                    {{c.Name}}
                FROM {{c}}
                WHERE {{c.Id}} = {{p.CategoryId}} AND {{c.IsActive}} = {{activeStatus}}
                """));
    }
}