// using SqlInterpol.Test.Dialects;
// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// public class JoinAsTests
// {
//     [Theory]
//     [MemberData(nameof(JoinWithLiteralAliasesData))]
//     public void Join_WithLiteralAliases(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
        
//         // Act
//         var result = db.Query<Product, OrderLine>((p, ol) =>
//             db.Append($$"""
//             SELECT
//                 {{p[x => x.Id]}},
//                 {{ol[x => x.OrderId]}}
//             FROM {{p}} AS p
//             JOIN {{ol}} AS ol
//                 ON {{p[x => x.Id]}} = {{ol[x => x.ProductItemNumber]}}
//             """))
//             .Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//     }

//     [Theory]
//     [MemberData(nameof(JoinWithExplicitApiAliasesData))]
//     public void Join_WithExplicitApiAliases(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
        
//         // Act
//         var result = db.Query<Product, OrderLine>((p, ol) =>
//             db.Append($$"""
//             SELECT
//                 {{p[x => x.Id]}},
//                 {{ol[x => x.OrderId]}}
//             FROM dbo.Products AS {{p.As("prod")}}
//             JOIN order_lines AS {{ol}}
//                 ON {{p[x => x.Id]}} = {{ol[x => x.ProductItemNumber]}}
//             """))
//             .Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//     }

//     [Theory]
//     [MemberData(nameof(SelfJoinData))]
//     public void Join_SelfJoin(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();

//         // Act
//         var result = db.Query<Product, Product>((p1, p2) =>
//             db.Append($$"""
//             SELECT
//                 {{p1[x => x.Id]}},
//                 {{p2[x => x.Id]}}
//             FROM {{p1}} AS original
//             JOIN {{p2}} AS related
//                 ON {{p1[x => x.CategoryId]}} = {{p2[x => x.CategoryId]}}
//             """))
//             .Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//     }

//     [Theory]
//     [MemberData(nameof(JoinWithConfigOverrideData))]
//     public void Join_WithSqlEntityConfig(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
        
//         // Act
//         var result = db.Entity<Product>(name: "Archive_Products", schema: "history")
//         .Entity<OrderLine>()
//         .Query((p, ol) =>
//             db.Append($$"""
//             SELECT
//                 {{p[x => x.Id]}},
//                 {{ol[x => x.OrderId]}}
//             FROM {{p}}
//             JOIN {{ol}}
//                 ON {{p[x => x.Id]}} = {{ol[x => x.ProductItemNumber]}}
//             """))
//             .Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//     }

//     public static TheoryData<SqlTestCase> JoinWithLiteralAliasesData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 SELECT
//                     p.<<Id>>,
//                     ol.<<OrderId>>
//                 FROM <<dbo>>.<<Products>> AS p
//                 JOIN <<OrderLine>> AS ol
//                     ON p.<<Id>> = ol.<<ProductItemNumber>>
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT
//                     p."Id",
//                     ol."OrderId"
//                 FROM "dbo"."Products" AS p
//                 JOIN "OrderLine" AS ol
//                     ON p."Id" = ol."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql, 
//             [
//                 """
//                 SELECT
//                     p.`Id`,
//                     ol.`OrderId`
//                 FROM `dbo`.`Products` AS p
//                 JOIN `OrderLine` AS ol
//                     ON p.`Id` = ol.`ProductItemNumber`
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle, 
//             [
//                 """
//                 SELECT
//                     p."Id",
//                     ol."OrderId"
//                 FROM "dbo"."Products" AS p
//                 JOIN "OrderLine" AS ol
//                     ON p."Id" = ol."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql, 
//             [
//                 """
//                 SELECT
//                     p."Id",
//                     ol."OrderId"
//                 FROM "dbo"."Products" AS p
//                 JOIN "OrderLine" AS ol
//                     ON p."Id" = ol."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 SELECT
//                     p."Id",
//                     ol."OrderId"
//                 FROM "dbo"."Products" AS p
//                 JOIN "OrderLine" AS ol
//                     ON p."Id" = ol."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT
//                     p.[Id],
//                     ol.[OrderId]
//                 FROM [dbo].[Products] AS p
//                 JOIN [OrderLine] AS ol
//                     ON p.[Id] = ol.[ProductItemNumber]
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> JoinWithExplicitApiAliasesData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb, 
//             [
//                 """
//                 SELECT
//                     <<prod>>.<<Id>>,
//                     <<OrderLine>>.<<OrderId>>
//                 FROM dbo.Products AS <<prod>>
//                 JOIN order_lines AS <<OrderLine>>
//                     ON <<prod>>.<<Id>> = <<OrderLine>>.<<ProductItemNumber>>
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT
//                     "prod"."Id",
//                     "OrderLine"."OrderId"
//                 FROM dbo.Products AS "prod"
//                 JOIN order_lines AS "OrderLine"
//                     ON "prod"."Id" = "OrderLine"."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql, 
//             [
//                 """
//                 SELECT
//                     `prod`.`Id`,
//                     `OrderLine`.`OrderId`
//                 FROM dbo.Products AS `prod`
//                 JOIN order_lines AS `OrderLine`
//                     ON `prod`.`Id` = `OrderLine`.`ProductItemNumber`
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle, 
//             [
//                 """
//                 SELECT
//                     "prod"."Id",
//                     "OrderLine"."OrderId"
//                 FROM dbo.Products AS "prod"
//                 JOIN order_lines AS "OrderLine"
//                     ON "prod"."Id" = "OrderLine"."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql, 
//             [
//                 """
//                 SELECT
//                     "prod"."Id",
//                     "OrderLine"."OrderId"
//                 FROM dbo.Products AS "prod"
//                 JOIN order_lines AS "OrderLine"
//                     ON "prod"."Id" = "OrderLine"."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 SELECT
//                     "prod"."Id",
//                     "OrderLine"."OrderId"
//                 FROM dbo.Products AS "prod"
//                 JOIN order_lines AS "OrderLine"
//                     ON "prod"."Id" = "OrderLine"."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT
//                     [prod].[Id],
//                     [OrderLine].[OrderId]
//                 FROM dbo.Products AS [prod]
//                 JOIN order_lines AS [OrderLine]
//                     ON [prod].[Id] = [OrderLine].[ProductItemNumber]
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> SelfJoinData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb, 
//             [
//                 """
//                 SELECT
//                     original.<<Id>>,
//                     related.<<Id>>
//                 FROM <<dbo>>.<<Products>> AS original
//                 JOIN <<dbo>>.<<Products>> AS related
//                     ON original.<<CategoryId>> = related.<<CategoryId>>
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT
//                     original."Id",
//                     related."Id"
//                 FROM "dbo"."Products" AS original
//                 JOIN "dbo"."Products" AS related
//                     ON original."CategoryId" = related."CategoryId"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql, 
//             [
//                 """
//                 SELECT
//                     original.`Id`,
//                     related.`Id`
//                 FROM `dbo`.`Products` AS original
//                 JOIN `dbo`.`Products` AS related
//                     ON original.`CategoryId` = related.`CategoryId`
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle, 
//             [
//                 """
//                 SELECT
//                     original."Id",
//                     related."Id"
//                 FROM "dbo"."Products" AS original
//                 JOIN "dbo"."Products" AS related
//                     ON original."CategoryId" = related."CategoryId"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql, 
//             [
//                 """
//                 SELECT
//                     original."Id",
//                     related."Id"
//                 FROM "dbo"."Products" AS original
//                 JOIN "dbo"."Products" AS related
//                     ON original."CategoryId" = related."CategoryId"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite, 
//             [
//                 """
//                 SELECT
//                     original."Id",
//                     related."Id"
//                 FROM "dbo"."Products" AS original
//                 JOIN "dbo"."Products" AS related
//                     ON original."CategoryId" = related."CategoryId"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT
//                     original.[Id],
//                     related.[Id]
//                 FROM [dbo].[Products] AS original
//                 JOIN [dbo].[Products] AS related
//                     ON original.[CategoryId] = related.[CategoryId]
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> JoinWithConfigOverrideData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 SELECT
//                     <<history>>.<<Archive_Products>>.<<Id>>,
//                     <<OrderLine>>.<<OrderId>>
//                 FROM <<history>>.<<Archive_Products>>
//                 JOIN <<OrderLine>>
//                     ON <<history>>.<<Archive_Products>>.<<Id>> = <<OrderLine>>.<<ProductItemNumber>>
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT
//                     "history"."Archive_Products"."Id",
//                     "OrderLine"."OrderId"
//                 FROM "history"."Archive_Products"
//                 JOIN "OrderLine"
//                     ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 SELECT
//                     `history`.`Archive_Products`.`Id`,
//                     `OrderLine`.`OrderId`
//                 FROM `history`.`Archive_Products`
//                 JOIN `OrderLine`
//                     ON `history`.`Archive_Products`.`Id` = `OrderLine`.`ProductItemNumber`
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle, 
//             [
//                 """
//                 SELECT
//                     "history"."Archive_Products"."Id",
//                     "OrderLine"."OrderId"
//                 FROM "history"."Archive_Products"
//                 JOIN "OrderLine"
//                     ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql, 
//             [
//                 """
//                 SELECT
//                     "history"."Archive_Products"."Id",
//                     "OrderLine"."OrderId"
//                 FROM "history"."Archive_Products"
//                 JOIN "OrderLine"
//                     ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite, 
//             [
//                 """
//                 SELECT
//                     "history"."Archive_Products"."Id",
//                     "OrderLine"."OrderId"
//                 FROM "history"."Archive_Products"
//                 JOIN "OrderLine"
//                     ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT
//                     [history].[Archive_Products].[Id],
//                     [OrderLine].[OrderId]
//                 FROM [history].[Archive_Products]
//                 JOIN [OrderLine]
//                     ON [history].[Archive_Products].[Id] = [OrderLine].[ProductItemNumber]
//                 """
//             ]
//         )
//     ];
// }