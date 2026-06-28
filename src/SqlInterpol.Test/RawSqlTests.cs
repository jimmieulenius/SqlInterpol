// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// public class RawSqlTests
// {
//     [Theory]
//     [MemberData(nameof(ComplexRawSqlData))]
//     public void RawSql_ComplexStatements_PassThroughUnmodified(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var minPrice = 50.00m;

//         // Act
//         // We are mixing AST nodes {{p}}, parameters {{minPrice}}, and RAW SQL here!
//         var result = db.Query<Product>(p => db.Append($$"""
//             SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
//             FROM {{p}}
//             WHERE {{p[x => x.Price]}} > {{minPrice}}
//               AND p.Status = 'ACTIVE' /* Raw SQL condition */
//             GROUP BY {{p[x => x.Id]}}, {{p[x => x.Name]}}
//             HAVING COUNT(*) > 1
//             ORDER BY {{p[x => x.Name]}} DESC
//             LIMIT 10 OFFSET 5
//             """)).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Single(result.Parameters);
//         Assert.Equal(minPrice, result.Parameters.First().Value);
//     }

//     // --- TEST DATA ---

//     public static TheoryData<SqlTestCase> ComplexRawSqlData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."Price" > @p0
//                   AND p.Status = 'ACTIVE' /* Raw SQL condition */
//                 GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 HAVING COUNT(*) > 1
//                 ORDER BY "dbo"."Products"."PROD_NAME" DESC
//                 LIMIT 10 OFFSET 5
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
//                 FROM `dbo`.`Products`
//                 WHERE `dbo`.`Products`.`Price` > @p0
//                   AND p.Status = 'ACTIVE' /* Raw SQL condition */
//                 GROUP BY `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
//                 HAVING COUNT(*) > 1
//                 ORDER BY `dbo`.`Products`.`PROD_NAME` DESC
//                 LIMIT 10 OFFSET 5
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."Price" > :0
//                   AND p.Status = 'ACTIVE' /* Raw SQL condition */
//                 GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 HAVING COUNT(*) > 1
//                 ORDER BY "dbo"."Products"."PROD_NAME" DESC
//                 LIMIT 10 OFFSET 5
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."Price" > $1
//                   AND p.Status = 'ACTIVE' /* Raw SQL condition */
//                 GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 HAVING COUNT(*) > 1
//                 ORDER BY "dbo"."Products"."PROD_NAME" DESC
//                 LIMIT 10 OFFSET 5
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."Price" > @p1
//                   AND p.Status = 'ACTIVE' /* Raw SQL condition */
//                 GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 HAVING COUNT(*) > 1
//                 ORDER BY "dbo"."Products"."PROD_NAME" DESC
//                 LIMIT 10 OFFSET 5
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
//                 FROM [dbo].[Products]
//                 WHERE [dbo].[Products].[Price] > @p0
//                   AND p.Status = 'ACTIVE' /* Raw SQL condition */
//                 GROUP BY [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
//                 HAVING COUNT(*) > 1
//                 ORDER BY [dbo].[Products].[PROD_NAME] DESC
//                 LIMIT 10 OFFSET 5
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> WindowFunctionData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT 
//                     "dbo"."Products"."PROD_NAME",
//                     "dbo"."Products"."Price",
//                     AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") as AvgCategoryPrice
//                 FROM "dbo"."Products"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 SELECT 
//                     `dbo`.`Products`.`PROD_NAME`,
//                     `dbo`.`Products`.`Price`,
//                     AVG(`dbo`.`Products`.`Price`) OVER (PARTITION BY `dbo`.`Products`.`CategoryId`) as AvgCategoryPrice
//                 FROM `dbo`.`Products`
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 SELECT 
//                     "dbo"."Products"."PROD_NAME",
//                     "dbo"."Products"."Price",
//                     AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") as AvgCategoryPrice
//                 FROM "dbo"."Products"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 SELECT 
//                     "dbo"."Products"."PROD_NAME",
//                     "dbo"."Products"."Price",
//                     AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") as AvgCategoryPrice
//                 FROM "dbo"."Products"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 SELECT 
//                     "dbo"."Products"."PROD_NAME",
//                     "dbo"."Products"."Price",
//                     AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") as AvgCategoryPrice
//                 FROM "dbo"."Products"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT 
//                     [dbo].[Products].[PROD_NAME],
//                     [dbo].[Products].[Price],
//                     AVG([dbo].[Products].[Price]) OVER (PARTITION BY [dbo].[Products].[CategoryId]) as AvgCategoryPrice
//                 FROM [dbo].[Products]
//                 """
//             ]
//         )
//     ];
// }