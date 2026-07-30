// using SqlInterpol.Configuration;
// using SqlInterpol.Test.Dialects;
// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// public abstract class LockSuite : LockSuiteBase
// {
//     static LockSuite()
//     {
//         SelectWithForUpdateTheory = new SqlTheory(() =>
//         {
//             object?[] expectedParams = [5];

//             return
//             [
//                 new SqlTestCase(
//                     SqlDialectKind.CustomDb,
//                     expectedExceptionType: typeof(SqlDialectException),
//                     expectedExceptionMessage: "The SQL dialect 'CustomDb' does not support the operation or fragment type: 'FOR UPDATE'."
//                 ),
//                 new SqlTestCase(
//                     SqlDialectKind.Firebird,
//                     [
//                         """
//                         SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                         FROM "dbo"."Products"
//                         WHERE "dbo"."Products"."Id" = @p0
//                         WITH LOCK
//                         """
//                     ],
//                     expectedParameters: expectedParams
//                 ),
//                 new SqlTestCase(
//                     SqlDialectKind.MySql,
//                     [
//                         """
//                         SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
//                         FROM `dbo`.`Products`
//                         WHERE `dbo`.`Products`.`Id` = @p0
//                         FOR UPDATE
//                         """
//                     ],
//                     expectedParameters: expectedParams
//                 ),
//                 new SqlTestCase(
//                     SqlDialectKind.Oracle,
//                     [
//                         """
//                         SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                         FROM "dbo"."Products"
//                         WHERE "dbo"."Products"."Id" = :0
//                         FOR UPDATE
//                         """
//                     ],
//                     expectedParameters: expectedParams
//                 ),
//                 new SqlTestCase(
//                     SqlDialectKind.PostgreSql,
//                     [
//                         """
//                         SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
//                         FROM "dbo"."Products"
//                         WHERE "dbo"."Products"."Id" = $1
//                         FOR UPDATE
//                         """
//                     ],
//                     expectedParameters: expectedParams
//                 ),
//                 new SqlTestCase(
//                     SqlDialectKind.SqLite,
//                     expectedExceptionType: typeof(SqlDialectException),
//                     expectedExceptionMessage: "The SQL dialect 'SqLite' does not support the operation or fragment type: 'FOR UPDATE'."
//                 ),
//                 new SqlTestCase(
//                     SqlDialectKind.SqlServer,
//                     [
//                         """
//                         SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
//                         FROM [dbo].[Products] WITH (UPDLOCK)
//                         WHERE [dbo].[Products].[Id] = @p0
//                         """
//                     ],
//                     expectedParameters: expectedParams
//                 )
//             ];
//         });
//     }
// }