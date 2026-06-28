// using SqlInterpol.Test.Dialects;
// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// public class PagingTests
// {
//     [Theory]
//     [MemberData(nameof(Paging_WithImplicitLimitOffsetData))]
//     public void Paging_WithImplicitLimitOffset(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         int pageSize = 10;
//         int pageOffset = 20;

//         // Act - Using standard PostgreSQL/MySQL syntax
//         var result = db.Query<Product>(p => db.Append($$"""
//             SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
//             FROM {{p}}
//             ORDER BY {{p[x => x.Id]}}
//             LIMIT {{pageSize}} OFFSET {{pageOffset}}
//             """)).Build();

//         // Assert SQL
//         testCase.AssertSql(result.Sql);

//         // Assert Parameters
//         // Even though SQL Server/Oracle swap the rendering order, 
//         // the ordinal values mapped to the DB must remain exactly as declared!
//         Assert.Equal(2, result.Parameters.Count);
//         Assert.Equal(10, result.Parameters.ElementAt(0).Value); // Limit / PageSize
//         Assert.Equal(20, result.Parameters.ElementAt(1).Value); // Offset
//     }

//     public static TheoryData<SqlTestCase> Paging_WithImplicitLimitOffsetData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
//                 FROM <<dbo>>.<<Products>>
//                 ORDER BY <<dbo>>.<<Products>>.<<Id>>
//                 LIMIT !!100 OFFSET !!101
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 ORDER BY "dbo"."Products"."Id"
//                 FIRST @p0 SKIP @p1
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
//                 FROM `dbo`.`Products`
//                 ORDER BY `dbo`.`Products`.`Id`
//                 LIMIT @p0 OFFSET @p1
//                 """
//             ]
//         ),
//         // Oracle (Swaps the visual order and applies ANSI syntax)
//         // Notice how :1 comes before :0!
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 ORDER BY "dbo"."Products"."Id"
//                 OFFSET :1 ROWS FETCH NEXT :0 ROWS ONLY
//                 """
//         ]),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 ORDER BY "dbo"."Products"."Id"
//                 LIMIT $1 OFFSET $2
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 ORDER BY "dbo"."Products"."Id"
//                 LIMIT @p1 OFFSET @p2
//                 """
//         ]),
//         // SQL Server (Swaps the visual order and applies ANSI syntax)
//         // Notice how @p1 comes before @p0!
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
//                 FROM [dbo].[Products]
//                 ORDER BY [dbo].[Products].[Id]
//                 OFFSET @p1 ROWS FETCH NEXT @p0 ROWS ONLY
//                 """
//             ]
//         )
//     ];
// }