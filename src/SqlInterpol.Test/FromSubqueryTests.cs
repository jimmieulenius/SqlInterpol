// using SqlInterpol.Test.Dialects;
// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// public class FromSubqueryTests
// {
//     [Theory]
//     [MemberData(nameof(From_SubqueryData))]
//     public void From_Subquery(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();

//         db
//             .Entity<CategoryStats>(out var stats)
//             .Entity<Product>(out var p, "p")
//             .Subquery(
//                 stats,
//                 () => db.Append($$"""
//                     SELECT
//                         {{p.CategoryId}} AS {{stats.CategoryId:col}},
//                         SUM({{p.Price}}) AS {{stats.TotalPrice:col}}
//                     FROM {{p}}
//                     GROUP BY {{p.CategoryId}}
//                 """));

//         // Act
//         var result = db
//             .Entity<Category>(out var c, "c")
//             .Append($$"""
//                 SELECT
//                     {{c.Name}},
//                     {{stats.TotalPrice}}
//                 FROM
//                 (
//                 {{stats}}
//                 ) AS {{"stats"}}
//                 JOIN {{c}} ON {{stats.CategoryId}} = {{c.Id}}
//                 """)
//             .Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//     }

//     [Theory]
//     [MemberData(nameof(From_SubqueryData))]
//     public void From_Subquery_AutoAliasing(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         db.Context.Options.EntityAutoAliasing = true;

//         db
//             .Entity<CategoryStats>(out var stats)
//             .Entity<Product>(out var p)
//             .Subquery(
//                 stats,
//                 () => db.Append($$"""
//                     SELECT
//                         {{p.CategoryId}} AS {{stats.CategoryId:col}},
//                         SUM({{p.Price}}) AS {{stats.TotalPrice:col}}
//                     FROM {{p}}
//                     GROUP BY {{p.CategoryId}}
//                     """));

//         // Act
//         var result = db
//             .Entity<Category>(out var c)
//             .Append($$"""
//                 SELECT
//                     {{c.Name}},
//                     {{stats.TotalPrice}}
//                 FROM
//                 {{stats}}
//                 JOIN {{c:decl}} ON {{stats.CategoryId}} = {{c.Id}}
//                 """)
//             .Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//     }

//     public static TheoryData<SqlTestCase> From_SubqueryData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 SELECT
//                     <<c>>.<<Name>>,
//                     <<stats>>.<<TotalPrice>>
//                 FROM
//                 (
//                     SELECT
//                         <<p>>.<<CategoryId>> AS <<CategoryId>>,
//                         SUM(<<p>>.<<Price>>) AS <<TotalPrice>>
//                     FROM <<dbo>>.<<Products>> AS <<p>>
//                     GROUP BY <<p>>.<<CategoryId>>
//                 ) AS <<stats>>
//                 JOIN <<Category>> AS <<c>> ON <<stats>>.<<CategoryId>> = <<c>>.<<Id>>
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT
//                     "c"."Name",
//                     "stats"."TotalPrice"
//                 FROM
//                 (
//                     SELECT
//                         "p"."CategoryId" AS "CategoryId",
//                         SUM("p"."Price") AS "TotalPrice"
//                     FROM "dbo"."Products" AS "p"
//                     GROUP BY "p"."CategoryId"
//                 ) AS "stats"
//                 JOIN "Category" AS "c" ON "stats"."CategoryId" = "c"."Id"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 SELECT
//                     `c`.`Name`,
//                     `stats`.`TotalPrice`
//                 FROM
//                 (
//                     SELECT
//                         `p`.`CategoryId` AS `CategoryId`,
//                         SUM(`p`.`Price`) AS `TotalPrice`
//                     FROM `dbo`.`Products` AS `p`
//                     GROUP BY `p`.`CategoryId`
//                 ) AS `stats`
//                 JOIN `Category` AS `c` ON `stats`.`CategoryId` = `c`.`Id`
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 SELECT
//                     "c"."Name",
//                     "stats"."TotalPrice"
//                 FROM
//                 (
//                     SELECT
//                         "p"."CategoryId" AS "CategoryId",
//                         SUM("p"."Price") AS "TotalPrice"
//                     FROM "dbo"."Products" AS "p"
//                     GROUP BY "p"."CategoryId"
//                 ) AS "stats"
//                 JOIN "Category" AS "c" ON "stats"."CategoryId" = "c"."Id"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 SELECT
//                     "c"."Name",
//                     "stats"."TotalPrice"
//                 FROM
//                 (
//                     SELECT
//                         "p"."CategoryId" AS "CategoryId",
//                         SUM("p"."Price") AS "TotalPrice"
//                     FROM "dbo"."Products" AS "p"
//                     GROUP BY "p"."CategoryId"
//                 ) AS "stats"
//                 JOIN "Category" AS "c" ON "stats"."CategoryId" = "c"."Id"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 SELECT
//                     "c"."Name",
//                     "stats"."TotalPrice"
//                 FROM
//                 (
//                     SELECT
//                         "p"."CategoryId" AS "CategoryId",
//                         SUM("p"."Price") AS "TotalPrice"
//                     FROM "dbo"."Products" AS "p"
//                     GROUP BY "p"."CategoryId"
//                 ) AS "stats"
//                 JOIN "Category" AS "c" ON "stats"."CategoryId" = "c"."Id"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT
//                     [c].[Name],
//                     [stats].[TotalPrice]
//                 FROM
//                 (
//                     SELECT
//                         [p].[CategoryId] AS [CategoryId],
//                         SUM([p].[Price]) AS [TotalPrice]
//                     FROM [dbo].[Products] AS [p]
//                     GROUP BY [p].[CategoryId]
//                 ) AS [stats]
//                 JOIN [Category] AS [c] ON [stats].[CategoryId] = [c].[Id]
//                 """
//             ]
//         )
//     ];
// }