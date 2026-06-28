// using SqlInterpol.Test.Dialects;
// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// public class SelectTemplateTests
// {
//     [Theory]
//     [MemberData(nameof(BuiltInTemplateSelectWithCriteriaData))]
//     public void Select_BuiltInTemplate_WithCriteria(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var criteria = new { Id = 100 };

//         // Act
//         // TODO: Migrate SqlTemplate pipelines to the new architecture
//         var result = db.Query<Product>(p =>
//             db.Append(SqlTemplate.Select<Product, ProductTemplateDto>(x => x.Id), p, criteria)
//         ).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Single(result.Parameters);
//         Assert.Equal(100, result.Parameters.Values.First());
//     }

//     [Theory]
//     [MemberData(nameof(CustomTemplateSelectData))]
//     public void Select_CustomTemplate(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var template = SqlTemplate.Create<Product>((builder, p) =>
//         {
//             builder.Append($"SELECT {p[x => x.Id]}, {p[x => x.Name]} FROM {p} WHERE {p[x => x.CategoryId]} = {Sql.Arg<Product>(x => x.CategoryId)}");
//         });
//         var args = new { CategoryId = 100 };

//         // Act
//         // TODO: Migrate custom SqlTemplate to the new architecture
//         var result = db.Query<Product>(p =>
//             db.Append(template, p, args)
//         ).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Single(result.Parameters);
//         Assert.Equal(100, result.Parameters.Values.First());
//     }

//     [Theory]
//     [MemberData(nameof(TemplateFragmentSelectData))]
//     public void Select_TemplateFragment(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var selectTemplate = SqlTemplate.Select<Product, ProductTemplateDto>();
//         var categoryId = 100;

//         // Act
//         // TODO: Migrate fragment appending to the new architecture
//         var result = db.Query<Product>(p =>
//             db.AppendLine(selectTemplate, p)
//             .Append($"WHERE {p[x => x.CategoryId]} = {categoryId}")
//         ).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Single(result.Parameters);
//         Assert.Equal(100, result.Parameters.Values.First());
//     }

//     public static TheoryData<SqlTestCase> BuiltInTemplateSelectWithCriteriaData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<IsActive>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
//                 FROM <<dbo>>.<<Products>>
//                 WHERE <<dbo>>.<<Products>>.<<Id>> = !!100
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."Id" = @p0
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`IsActive`, `dbo`.`Products`.`PROD_NAME`
//                 FROM `dbo`.`Products`
//                 WHERE `dbo`.`Products`.`Id` = @p0
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."Id" = :0
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."Id" = $1
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."Id" = @p1
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT [dbo].[Products].[Id], [dbo].[Products].[IsActive], [dbo].[Products].[PROD_NAME]
//                 FROM [dbo].[Products]
//                 WHERE [dbo].[Products].[Id] = @p0
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> CustomTemplateSelectData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>> FROM <<dbo>>.<<Products>> WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = @p0
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME` FROM `dbo`.`Products` WHERE `dbo`.`Products`.`CategoryId` = @p0
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = :0
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = $1
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = @p1
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME] FROM [dbo].[Products] WHERE [dbo].[Products].[CategoryId] = @p0
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> TemplateFragmentSelectData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<IsActive>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
//                 FROM <<dbo>>.<<Products>>
//                 WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."CategoryId" = @p0
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`IsActive`, `dbo`.`Products`.`PROD_NAME`
//                 FROM `dbo`.`Products`
//                 WHERE `dbo`.`Products`.`CategoryId` = @p0
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."CategoryId" = :0
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."CategoryId" = $1
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
//                 FROM "dbo"."Products"
//                 WHERE "dbo"."Products"."CategoryId" = @p1
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 SELECT [dbo].[Products].[Id], [dbo].[Products].[IsActive], [dbo].[Products].[PROD_NAME]
//                 FROM [dbo].[Products]
//                 WHERE [dbo].[Products].[CategoryId] = @p0
//                 """
//             ]
//         )
//     ];
// }