// using SqlInterpol.Test.Dialects;
// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// public class UpdateTests
// {
//     [Theory]
//     [MemberData(nameof(UpdateData))]
//     public void Update_WithContextualDto(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var updateDto = new { Status = "Shipped", Total = 99.99m };
//         int orderId = 42;

//         // Act - Using the new contextual parser!
//         var result = db.Query<OrderModel>(o => db.Append($$"""
//             UPDATE {{o}}
//             SET {{updateDto}}
//             WHERE {{o[x => x.Id]}} = {{orderId}}
//             """)).Build();

//         // Assert SQL
//         testCase.AssertSql(result.Sql);

//         // Assert Parameters
//         Assert.Equal(3, result.Parameters.Count);
//         Assert.Equal("Shipped", result.Parameters.ElementAt(0).Value);
//         Assert.Equal(99.99m, result.Parameters.ElementAt(1).Value);
//         Assert.Equal(42, result.Parameters.ElementAt(2).Value);
//     }

//     [Theory]
//     [MemberData(nameof(UpdateExplicitData))]
//     public void Update_PureManual(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var status = "Shipped";
//         var total = 99.99m;
//         int orderId = 42;
        
//         // Act - Pure raw SQL mapping
//         var result = db.Query<OrderModel>(o => db.Append($$"""
//             UPDATE {{o}}
//             SET {{o[x => x.Status]}} = {{status}}, {{o[x => x.Total]}} = {{total}}
//             WHERE {{o[x => x.Id]}} = {{orderId}}
//             """)).Build();

//         // Assert SQL
//         testCase.AssertSql(result.Sql);
        
//         // Assert Parameters
//         Assert.Equal(3, result.Parameters.Count);
//         Assert.Equal(status, result.Parameters.ElementAt(0).Value);
//         Assert.Equal(total, result.Parameters.ElementAt(1).Value);
//         Assert.Equal(orderId, result.Parameters.ElementAt(2).Value);
//     }

//     [Theory]
//     [MemberData(nameof(UpdateWithIgnoreData))]
//     public void Update_WithIgnoredProperty(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var order = new OrderWithIgnoreModel 
//         { 
//             Id = 42, 
//             Status = "Shipped", 
//             Total = 99.99m, 
//             InternalNotes = "Ignore me!" 
//         };

//         // Act - Passing the full object which triggers BuildAssignments
//         var result = db.Query<OrderWithIgnoreModel>(o => db.Append($$"""
//             UPDATE {{o}}
//             SET {{order}}
//             WHERE {{o[x => x.Id]}} = {{order.Id}}
//             """)).Build();

//         // Assert SQL
//         testCase.AssertSql(result.Sql);
        
//         // Assert Parameters - 3 parameters from SET (Id, Status, Total) + 1 from WHERE (Id) = 4 total
//         Assert.Equal(4, result.Parameters.Count);
//         Assert.Equal(42, result.Parameters.ElementAt(0).Value);
//         Assert.Equal("Shipped", result.Parameters.ElementAt(1).Value);
//         Assert.Equal(99.99m, result.Parameters.ElementAt(2).Value);
//         Assert.Equal(42, result.Parameters.ElementAt(3).Value);
//     }

//     [Theory]
//     [MemberData(nameof(UpdateErrorData))]
//     public void Update_ValidationRules(SqlErrorTestCase testCase)
//     {
//         // Act
//         var exception = Record.Exception(() => 
//         {
//             var db = testCase.CreateBuilder();

//             if (testCase.ExpectedMessageSubstring.Contains("implement"))
//             {
//                 Sql.BuildAssignments(new InvalidDummyEntity(), new { Name = "Test" }, db.Context);
//             }
//             else
//             {
                
//                 var entity = db.AddEntity<Product>();
//                 #pragma warning disable SQLI002
//                 Sql.BuildAssignments(entity, new { Id = 1, NonExistentProperty = "Should Fail" }, db.Context);
//                 #pragma warning restore SQLI002
//             }
//         });

//         // Assert
//         testCase.AssertException(exception);
//     }

//     [Theory]
//     [MemberData(nameof(MultiTableUpdateData))]
//     public void Update_MultiTable_TranslatesAcrossDialects(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();

//         // Act - Implicit Join WYSIWYG!
//         var result = db
//             .Entity<Product>()
//             .Entity<Category>()
//             .Query((p, c) =>
//         db.Append($$"""
//             UPDATE {{p}}
//             SET {{p[x => x.Price]}} = {{10}}
//             FROM {{c}} AS c1
//             WHERE {{p[x => x.CategoryId]}} = c1.Id
//             """)).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//     }

//     [Theory]
//     [MemberData(nameof(UpdateTemplateData))]
//     public void AppendUpdate_Template(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var user = new TestUser { Id = 1, Name = "Bob", Age = 31 };

//         // Act
//         var result = db.Query<TestUser>(u => 
//             db.AppendUpdate(u, user, x => x.Id)
//         ).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Equal(3, result.Parameters.Count);
//     }

//     public static TheoryData<SqlTestCase> UpdateData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 UPDATE <<dbo>>.<<Orders>>
//                 SET <<order_status>> = !!100, <<Total>> = !!101
//                 WHERE <<dbo>>.<<Orders>>.<<Id>> = !!102
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 UPDATE `dbo`.`Orders`
//                 SET `order_status` = @p0, `Total` = @p1
//                 WHERE `dbo`.`Orders`.`Id` = @p2
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 UPDATE "dbo"."Orders"
//                 SET "order_status" = :0, "Total" = :1
//                 WHERE "dbo"."Orders"."Id" = :2
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 UPDATE "dbo"."Orders"
//                 SET "order_status" = $1, "Total" = $2
//                 WHERE "dbo"."Orders"."Id" = $3
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 UPDATE "dbo"."Orders"
//                 SET "order_status" = @p1, "Total" = @p2
//                 WHERE "dbo"."Orders"."Id" = @p3
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 UPDATE [dbo].[Orders]
//                 SET [order_status] = @p0, [Total] = @p1
//                 WHERE [dbo].[Orders].[Id] = @p2
//                 """
//             ]
//         )
//     ];

//     // Expected data for the Explicit/Manual tests (Notice how they render slightly differently 
//     // because manual uses fully qualified aliases like [dbo].[Orders].[order_status] = ...)
//     public static TheoryData<SqlTestCase> UpdateExplicitData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 UPDATE <<dbo>>.<<Orders>>
//                 SET <<dbo>>.<<Orders>>.<<order_status>> = !!100, <<dbo>>.<<Orders>>.<<Total>> = !!101
//                 WHERE <<dbo>>.<<Orders>>.<<Id>> = !!102
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 UPDATE `dbo`.`Orders`
//                 SET `dbo`.`Orders`.`order_status` = @p0, `dbo`.`Orders`.`Total` = @p1
//                 WHERE `dbo`.`Orders`.`Id` = @p2
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 UPDATE "dbo"."Orders"
//                 SET "dbo"."Orders"."order_status" = :0, "dbo"."Orders"."Total" = :1
//                 WHERE "dbo"."Orders"."Id" = :2
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 UPDATE "dbo"."Orders"
//                 SET "dbo"."Orders"."order_status" = $1, "dbo"."Orders"."Total" = $2
//                 WHERE "dbo"."Orders"."Id" = $3
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 UPDATE "dbo"."Orders"
//                 SET "dbo"."Orders"."order_status" = @p1, "dbo"."Orders"."Total" = @p2
//                 WHERE "dbo"."Orders"."Id" = @p3
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 UPDATE [dbo].[Orders]
//                 SET [dbo].[Orders].[order_status] = @p0, [dbo].[Orders].[Total] = @p1
//                 WHERE [dbo].[Orders].[Id] = @p2
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> UpdateWithIgnoreData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 UPDATE <<dbo>>.<<Orders>>
//                 SET <<Id>> = !!100, <<order_status>> = !!101, <<Total>> = !!102
//                 WHERE <<dbo>>.<<Orders>>.<<Id>> = !!103
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 UPDATE `dbo`.`Orders`
//                 SET `Id` = @p0, `order_status` = @p1, `Total` = @p2
//                 WHERE `dbo`.`Orders`.`Id` = @p3
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 UPDATE "dbo"."Orders"
//                 SET "Id" = :0, "order_status" = :1, "Total" = :2
//                 WHERE "dbo"."Orders"."Id" = :3
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 UPDATE "dbo"."Orders"
//                 SET "Id" = $1, "order_status" = $2, "Total" = $3
//                 WHERE "dbo"."Orders"."Id" = $4
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 UPDATE "dbo"."Orders"
//                 SET "Id" = @p1, "order_status" = @p2, "Total" = @p3
//                 WHERE "dbo"."Orders"."Id" = @p4
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 UPDATE [dbo].[Orders]
//                 SET [Id] = @p0, [order_status] = @p1, [Total] = @p2
//                 WHERE [dbo].[Orders].[Id] = @p3
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlErrorTestCase> UpdateErrorData =>
//     [
//         new SqlErrorTestCase(
//             SqlDialectKind.CustomDb,
//             typeof(ArgumentException),
//             "Entity must implement ISqlEntityBase<T>"
//         ),
//         new SqlErrorTestCase(
//             SqlDialectKind.CustomDb,
//             typeof(ArgumentException),
//             "Property 'NonExistentProperty' on DTO does not exist on Entity."
//         )
//     ];

//     public static TheoryData<SqlTestCase> MultiTableUpdateData =>
//     [
//         // CRITICAL CHECK: MySQL dynamically rearranges the AST into "UPDATE A, B SET A.Price = X WHERE..."
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 UPDATE `dbo`.`Products`, `Category` AS c1
//                 SET `Price` = @p0
//                 WHERE `dbo`.`Products`.`CategoryId` = c1.Id
//                 """
//             ]
//         ),
//         // CRITICAL CHECK: Oracle uses MERGE for multi-table updates, so the syntax is very different!
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 MERGE INTO "dbo"."Products"
//                 USING "Category" c1
//                 ON ("dbo"."Products"."CategoryId" = c1.Id)
//                 WHEN MATCHED THEN UPDATE SET "Price" = :0
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 UPDATE "dbo"."Products"
//                 SET "Price" = $1
//                 FROM "Category" AS c1
//                 WHERE "dbo"."Products"."CategoryId" = c1.Id
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 UPDATE "dbo"."Products"
//                 SET "Price" = @p1
//                 FROM "Category" AS c1
//                 WHERE "dbo"."Products"."CategoryId" = c1.Id
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 UPDATE [dbo].[Products]
//                 SET [Price] = @p0
//                 FROM [Category] AS c1
//                 WHERE [dbo].[Products].[CategoryId] = c1.Id
//                 """
//             ]
//         ),
//     ];

//     public static TheoryData<SqlTestCase> UpdateTemplateData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 UPDATE <<Users>>
//                 SET <<Age>> = !!100, <<Name>> = !!101
//                 WHERE <<Users>>.<<Id>> = !!102
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 UPDATE "Users"
//                 SET "Age" = @p0, "Name" = @p1
//                 WHERE "Users"."Id" = @p2
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 UPDATE `Users`
//                 SET `Age` = @p0, `Name` = @p1
//                 WHERE `Users`.`Id` = @p2
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 UPDATE "Users"
//                 SET "Age" = :0, "Name" = :1
//                 WHERE "Users"."Id" = :2
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 UPDATE "Users"
//                 SET "Age" = $1, "Name" = $2
//                 WHERE "Users"."Id" = $3
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 UPDATE "Users"
//                 SET "Age" = @p1, "Name" = @p2
//                 WHERE "Users"."Id" = @p3
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 UPDATE [Users]
//                 SET [Age] = @p0, [Name] = @p1
//                 WHERE [Users].[Id] = @p2
//                 """
//             ]
//         )
//     ];
// }