// using SqlInterpol.Test.Dialects;
// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// public class InsertTests
// {
//     [Theory]
//     [MemberData(nameof(InsertData))]
//     public void Insert_WithImplicitSyntax(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

//         // Act
//         var result = db.Query<Product>(p => db.Append($$"""
//             INSERT INTO {{p}}
//             {{newProduct}}
//             """)).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Equal(3, result.Parameters.Count);
//         Assert.Equal("Test Product", result.Parameters.ElementAt(0).Value);
//         Assert.Equal(5, result.Parameters.ElementAt(1).Value);
//         Assert.Equal(19.99m, result.Parameters.ElementAt(2).Value);
//     }

//     [Theory]
//     [MemberData(nameof(InsertData))]
//     public void Insert_WithExplicitValuesKeyword(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

//         // Act
//         var result = db.Query<Product>(p => db.Append($$"""
//             INSERT INTO {{p}}
//             VALUES {{newProduct}}
//             """)).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Equal(3, result.Parameters.Count);
//     }

//     [Theory]
//     [MemberData(nameof(ManualInsertData))]
//     public void Insert_PureManual(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var status = "Manual";
//         var total = 100.00m;
        
//         // Act
//         var result = db.Query<OrderModel>(o =>
//             db.Append($$"""
//             INSERT INTO {{o}}
//             ({{o[x => x.Status]}}, {{o[x => x.Total]}})
//             VALUES ({{status}}, {{total}})
//             """))
//             .Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Equal(status, result.Parameters.ElementAt(0).Value);
//         Assert.Equal(total, result.Parameters.ElementAt(1).Value);
//     }

//     [Theory]
//     [MemberData(nameof(BulkInsertData))]
//     public void Insert_BulkImplicitSyntax(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var products = new[]
//         {
//             new { Name = "Prod1", CategoryId = 1, Price = 10m },
//             new { Name = "Prod2", CategoryId = 2, Price = 20m }
//         };

//         // Act
//         var result = db.Query<Product>(p => db.Append($$"""
//             INSERT INTO {{p}} VALUES {{products}}
//             """)).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Equal(6, result.Parameters.Count);
//     }

//     [Theory]
//     [MemberData(nameof(ReturningSingleData))]
//     public void Insert_ReturningSingleColumn(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

//         // Act
//         var result = db.Query<Product>(p => db.Append($$"""
//             INSERT INTO {{p}} {{newProduct}}
//             RETURNING {{p[x => x.Id]}}
//             """)).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//     }

//     [Theory]
//     [MemberData(nameof(ReturningMultipleData))]
//     public void Insert_ReturningMultipleColumns(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

//         // Act
//         var result = db.Query<Product>(p => db.Append($$"""
//             INSERT INTO {{p}} {{newProduct}}
//             RETURNING {{p[x => x.Id]}}, {{p[x => x.Name]}}
//             """)).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//     }

//     [Theory]
//     [MemberData(nameof(UnsupportedReturningData))]
//     public void Insert_Returning_ThrowsException_ForUnsupportedDialect(SqlErrorTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

//         // Act
//         var exception = Record.Exception(() => 
//         {
//             db.Query<Product>(p => db.Append($$"""
//                 INSERT INTO {{p}} {{newProduct}}
//                 RETURNING {{p[x => x.Id]}}
//                 """)).Build();
//         });

//         // Assert
//         testCase.AssertException(exception);
//     }

//     [Theory]
//     [MemberData(nameof(InsertWithIgnoreData))]
//     public void Insert_WithIgnoredProperty(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var product = new ProductWithIgnoreModel { Id = 1, Name = "Gadget", RuntimeCacheToken = "OmitThisColumn" };

//         // Act
//         var result = db.Query<ProductWithIgnoreModel>(p => db.Append($$"""
//             INSERT INTO {{p}} {{product}}
//             """)).Build();

//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Equal(2, result.Parameters.Count);
//     }

//     [Theory]
//     [MemberData(nameof(InsertComplexData))]
//     public void Insert_RespectsEnumFormats_AndIgnoresComplexTypes(SqlTestCase testCase)
//     {
//         // Arrange
//         var options = new SqlInterpolOptions { EnumFormat = SqlEnumFormat.Integer };
        
//         // Assuming testCase.CreateBuilder accepts options. If not, just instantiate it:
//         // var db = new SqlBuilder(testCase.Dialect, options);
//         var db = testCase.CreateBuilder(options); 

//         var p = db.AddEntity<ComplexProduct>();

//         var product = new ComplexProduct
//         {
//             Id = 42,
//             Name = "Mechanical Keyboard",
//             Status = ProductStatus.Available,
//             Category = ProductCategoryType.Electronics,
//             Supplier = new Supplier { Id = 99, Name = "TechCorp" }
//         };

//         // Act
//         var result = db.Append($$"""
//             INSERT INTO {{p}}
//             VALUES {{product}};
//             """).Build();

//         // Assert SQL Structure
//         testCase.AssertSql(result.Sql);

//         // Assert Parameters (4 parameters total, Supplier ignored)
//         Assert.Equal(4, result.Parameters.Count);
        
//         var parameters = result.Parameters.Values.ToList();
//         Assert.Equal(42, parameters[0]);
//         Assert.Equal("Mechanical Keyboard", parameters[1]);
        
//         // Status is an Integer (Global Default)
//         Assert.IsType<int>(parameters[2]);
//         Assert.Equal(1, parameters[2]);
        
//         // Category is a String (Property Override)
//         Assert.IsType<string>(parameters[3]);
//         Assert.Equal("Electronics", parameters[3]);
//     }

//     [Theory]
//     [MemberData(nameof(InsertTemplateData))]
//     public void AppendInsert_Template(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         var user = new TestUser { Id = 1, Name = "Alice", Age = 30 };
        
//         // Act
//         var result = db.Query<TestUser>(u =>
//             db.AppendInsert(u, user)
//         ).Build();
        
//         // Assert
//         testCase.AssertSql(result.Sql);
//         Assert.Equal(3, result.Parameters.Count);
//     }

//     public static TheoryData<SqlTestCase> InsertData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 INSERT INTO <<dbo>>.<<Products>>
//                 (<<PROD_NAME>>, <<CategoryId>>, <<Price>>)
//                 VALUES (!!100, !!101, !!102)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 INSERT INTO "dbo"."Products"
//                 ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (@p0, @p1, @p2)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 INSERT INTO `dbo`.`Products`
//                 (`PROD_NAME`, `CategoryId`, `Price`)
//                 VALUES (@p0, @p1, @p2)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 INSERT INTO "dbo"."Products"
//                 ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (:0, :1, :2)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 INSERT INTO "dbo"."Products"
//                 ("PROD_NAME", "CategoryId", "Price")
//                 VALUES ($1, $2, $3)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 INSERT INTO "dbo"."Products"
//                 ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (@p1, @p2, @p3)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 INSERT INTO [dbo].[Products]
//                 ([PROD_NAME], [CategoryId], [Price])
//                 VALUES (@p0, @p1, @p2)
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> ManualInsertData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 INSERT INTO <<dbo>>.<<Orders>>
//                 (<<order_status>>, <<Total>>)
//                 VALUES (!!100, !!101)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 INSERT INTO "dbo"."Orders"
//                 ("order_status", "Total")
//                 VALUES (@p0, @p1)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 INSERT INTO `dbo`.`Orders`
//                 (`order_status`, `Total`)
//                 VALUES (@p0, @p1)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 INSERT INTO "dbo"."Orders"
//                 ("order_status", "Total")
//                 VALUES (:0, :1)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 INSERT INTO "dbo"."Orders"
//                 ("order_status", "Total")
//                 VALUES ($1, $2)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 INSERT INTO "dbo"."Orders"
//                 ("order_status", "Total")
//                 VALUES (@p1, @p2)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 INSERT INTO [dbo].[Orders]
//                 ([order_status], [Total])
//                 VALUES (@p0, @p1)
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> BulkInsertData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 INSERT INTO <<dbo>>.<<Products>> (<<PROD_NAME>>, <<CategoryId>>, <<Price>>)
//                 VALUES (!!100, !!101, !!102), (!!103, !!104, !!105)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 INSERT INTO `dbo`.`Products` (`PROD_NAME`, `CategoryId`, `Price`)
//                 VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (:0, :1, :2), (:3, :4, :5)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES ($1, $2, $3), ($4, $5, $6)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (@p1, @p2, @p3), (@p4, @p5, @p6)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 INSERT INTO [dbo].[Products] ([PROD_NAME], [CategoryId], [Price])
//                 VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> ReturningSingleData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (@p0, @p1, @p2)
//                 RETURNING "Id"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (:0, :1, :2)
//                 RETURNING "Id"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES ($1, $2, $3)
//                 RETURNING "Id"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (@p1, @p2, @p3)
//                 RETURNING "Id"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 INSERT INTO [dbo].[Products] ([PROD_NAME], [CategoryId], [Price])
//                 OUTPUT inserted.[Id]
//                 VALUES (@p0, @p1, @p2)
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> ReturningMultipleData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (@p0, @p1, @p2)
//                 RETURNING "Id", "PROD_NAME"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (:0, :1, :2)
//                 RETURNING "Id", "PROD_NAME"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES ($1, $2, $3)
//                 RETURNING "Id", "PROD_NAME"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
//                 VALUES (@p1, @p2, @p3)
//                 RETURNING "Id", "PROD_NAME"
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 INSERT INTO [dbo].[Products] ([PROD_NAME], [CategoryId], [Price])
//                 OUTPUT inserted.[Id], inserted.[PROD_NAME]
//                 VALUES (@p0, @p1, @p2)
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlErrorTestCase> UnsupportedReturningData =>
//     [
//         new SqlErrorTestCase(SqlDialectKind.CustomDb, typeof(SqlDialectException), "'RETURNING' is not supported"),
//         new SqlErrorTestCase(SqlDialectKind.MySql, typeof(SqlDialectException), "'RETURNING' is not supported")
//     ];

//     public static TheoryData<SqlTestCase> InsertWithIgnoreData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 INSERT INTO <<dbo>>.<<Products>> (<<Id>>, <<PROD_NAME>>)
//                 VALUES (!!100, !!101)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("Id", "PROD_NAME")
//                 VALUES (@p0, @p1)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 INSERT INTO `dbo`.`Products` (`Id`, `PROD_NAME`)
//                 VALUES (@p0, @p1)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("Id", "PROD_NAME")
//                 VALUES (:0, :1)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("Id", "PROD_NAME")
//                 VALUES ($1, $2)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 INSERT INTO "dbo"."Products" ("Id", "PROD_NAME")
//                 VALUES (@p1, @p2)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 INSERT INTO [dbo].[Products] ([Id], [PROD_NAME])
//                 VALUES (@p0, @p1)
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> InsertComplexData =>
//     [
//         new SqlTestCase
//         (
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 INSERT INTO <<tbl_complex_products>>
//                 (<<Id>>, <<Name>>, <<Status>>, <<Category>>)
//                 VALUES (!!0, !!1, !!2, !!3);
//                 """
//             ]
//         ),
//         new SqlTestCase
//         (
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 INSERT INTO "tbl_complex_products"
//                 ("Id", "Name", "Status", "Category")
//                 VALUES (@p0, @p1, @p2, @p3);
//                 """
//             ]
//         ),
//         new SqlTestCase
//         (
//             SqlDialectKind.MySql,
//             [
//                 """
//                 INSERT INTO `tbl_complex_products`
//                 (`Id`, `Name`, `Status`, `Category`)
//                 VALUES (@p0, @p1, @p2, @p3);
//                 """
//             ]
//         ),
//         new SqlTestCase
//         (
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 INSERT INTO "tbl_complex_products"
//                 ("Id", "Name", "Status", "Category")
//                 VALUES (:0, :1, :2, :3);
//                 """
//             ]
//         ),
//         new SqlTestCase
//         (
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 INSERT INTO "tbl_complex_products"
//                 ("Id", "Name", "Status", "Category")
//                 VALUES ($0, $1, $2, $3);
//                 """
//             ]
//         ),
//         new SqlTestCase
//         (
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 INSERT INTO "tbl_complex_products"
//                 ("Id", "Name", "Status", "Category")
//                 VALUES (@p0, @p1, @p2, @p3);
//                 """
//             ]
//         ),
//         new SqlTestCase
//         (
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 INSERT INTO [tbl_complex_products]
//                 ([Id], [Name], [Status], [Category])
//                 VALUES (@p0, @p1, @p2, @p3);
//                 """
//             ]
//         )
//     ];

//     public static TheoryData<SqlTestCase> InsertTemplateData =>
//     [
//         new SqlTestCase(
//             SqlDialectKind.CustomDb,
//             [
//                 """
//                 INSERT INTO <<Users>>
//                 (<<Age>>, <<Id>>, <<Name>>)
//                 VALUES (!!100, !!101, !!102)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Firebird,
//             [
//                 """
//                 INSERT INTO "Users"
//                 ("Age", "Id", "Name")
//                 VALUES (@p0, @p1, @p2)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.MySql,
//             [
//                 """
//                 INSERT INTO `Users`
//                 (`Age`, `Id`, `Name`)
//                 VALUES (@p0, @p1, @p2)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.Oracle,
//             [
//                 """
//                 INSERT INTO "Users"
//                 ("Age", "Id", "Name")
//                 VALUES (:0, :1, :2)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.PostgreSql,
//             [
//                 """
//                 INSERT INTO "Users"
//                 ("Age", "Id", "Name")
//                 VALUES ($1, $2, $3)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqLite,
//             [
//                 """
//                 INSERT INTO "Users"
//                 ("Age", "Id", "Name")
//                 VALUES (@p1, @p2, @p3)
//                 """
//             ]
//         ),
//         new SqlTestCase(
//             SqlDialectKind.SqlServer,
//             [
//                 """
//                 INSERT INTO [Users]
//                 ([Age], [Id], [Name])
//                 VALUES (@p0, @p1, @p2)
//                 """
//             ]
//         )
//     ];
// }