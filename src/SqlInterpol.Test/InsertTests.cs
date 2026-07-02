using System.Linq;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class InsertTests
{
    [Theory]
    [MemberData(nameof(InsertData))]
    public void Insert_WithImplicitSyntax(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            INSERT INTO {{p}}
            {{newProduct}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(InsertData))]
    public void Insert_WithExplicitValuesKeyword(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            INSERT INTO {{p}}
            VALUES {{newProduct}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(ManualInsertData))]
    public void Insert_PureManual(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var status = "Manual";
        var total = 100.00m;
        
        // Act
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$"""
            INSERT INTO {{o}}
            ({{o.Status}}, {{o.Total}})
            VALUES ({{status}}, {{total}})
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(BulkInsertData))]
    public void Insert_BulkImplicitSyntax(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var products = new[]
        {
            new { Name = "Prod1", CategoryId = 1, Price = 10m },
            new { Name = "Prod2", CategoryId = 2, Price = 20m }
        };

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            INSERT INTO {{p}} VALUES {{products}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(ReturningSingleData))]
    public void Insert_ReturningSingleColumn(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            INSERT INTO {{p}} {{newProduct}}
            RETURNING {{p.Id}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(ReturningMultipleData))]
    public void Insert_ReturningMultipleColumns(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            INSERT INTO {{p}} {{newProduct}}
            RETURNING {{p.Id}}, {{p.Name}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(UnsupportedReturningData))]
    public void Insert_Returning_ThrowsException_ForUnsupportedDialect(SqlErrorTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act
        var exception = Record.Exception(() => 
        {
            db.Entity<Product>(out var p)
                .Append($$"""
                INSERT INTO {{p}} {{newProduct}}
                RETURNING {{p.Id}}
                """)
                .Build();
        });

        // Assert
        testCase.AssertException(exception);
    }

    [Theory]
    [MemberData(nameof(InsertWithIgnoreData))]
    public void Insert_WithIgnoredProperty(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var product = new ProductWithIgnoreModel { Id = 1, Name = "Gadget", RuntimeCacheToken = "OmitThisColumn" };

        // Act
        testCase.Action(() => db.Entity<ProductWithIgnoreModel>(out var p)
            .Append($$"""
            INSERT INTO {{p}} {{product}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(InsertComplexData))]
    public void Insert_RespectsEnumFormats_AndIgnoresComplexTypes(SqlTestCase testCase)
    {
        // Arrange
        // Extract the default options for this dialect and safely override only the EnumFormat
        var tempDb = testCase.CreateBuilder();
        var options = tempDb.Context.Options with { EnumFormat = SqlEnumFormat.Integer };
        var db = testCase.CreateBuilder(options); 

        var product = new ComplexProduct
        {
            Id = 42,
            Name = "Mechanical Keyboard",
            Status = ProductStatus.Available,
            Category = ProductCategoryType.Electronics,
            Supplier = new Supplier { Id = 99, Name = "TechCorp" }
        };

        // Act
        testCase.Action(() => db.Entity<ComplexProduct>(out var p)
            .Append($$"""
            INSERT INTO {{p}}
            VALUES {{product}};
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    // TODO: Add tests using templates for Insert statements, including scenarios with ignored properties and complex types.
    // [Theory]
    // [MemberData(nameof(InsertTemplateData))]
    // public void AppendInsert_Template(SqlTestCase testCase)
    // {
    //     // Arrange
    //     var db = testCase.CreateBuilder();
    //     var user = new TestUser { Id = 1, Name = "Alice", Age = 30 };
        
    //     // Act
    //     testCase.Action(() => db.Entity<TestUser>(out var u)
    //         .AppendInsert(u, user)
    //         .Build()
    //     );
        
    //     // Assert
    //     testCase.Assert();
    // }

    public static TheoryData<SqlTestCase> InsertData
    {
        get
        {
            object?[] expectedParams = ["Test Product", 5, 19.99m];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        INSERT INTO <<dbo>>.<<Products>>
                        (<<PROD_NAME>>, <<CategoryId>>, <<Price>>)
                        VALUES (!!100, !!101, !!102)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        INSERT INTO "dbo"."Products"
                        ("PROD_NAME", "CategoryId", "Price")
                        VALUES (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        INSERT INTO `dbo`.`Products`
                        (`PROD_NAME`, `CategoryId`, `Price`)
                        VALUES (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        INSERT INTO "dbo"."Products"
                        ("PROD_NAME", "CategoryId", "Price")
                        VALUES (:0, :1, :2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        INSERT INTO "dbo"."Products"
                        ("PROD_NAME", "CategoryId", "Price")
                        VALUES ($1, $2, $3)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        INSERT INTO "dbo"."Products"
                        ("PROD_NAME", "CategoryId", "Price")
                        VALUES (@p1, @p2, @p3)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        INSERT INTO [dbo].[Products]
                        ([PROD_NAME], [CategoryId], [Price])
                        VALUES (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> ManualInsertData
    {
        get
        {
            object?[] expectedParams = ["Manual", 100.00m];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        INSERT INTO <<dbo>>.<<Orders>>
                        (<<order_status>>, <<Total>>)
                        VALUES (!!100, !!101)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        INSERT INTO "dbo"."Orders"
                        ("order_status", "Total")
                        VALUES (@p0, @p1)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        INSERT INTO `dbo`.`Orders`
                        (`order_status`, `Total`)
                        VALUES (@p0, @p1)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        INSERT INTO "dbo"."Orders"
                        ("order_status", "Total")
                        VALUES (:0, :1)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        INSERT INTO "dbo"."Orders"
                        ("order_status", "Total")
                        VALUES ($1, $2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        INSERT INTO "dbo"."Orders"
                        ("order_status", "Total")
                        VALUES (@p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        INSERT INTO [dbo].[Orders]
                        ([order_status], [Total])
                        VALUES (@p0, @p1)
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> BulkInsertData
    {
        get
        {
            object?[] expectedParams = ["Prod1", 1, 10m, "Prod2", 2, 20m];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        INSERT INTO <<dbo>>.<<Products>> (<<PROD_NAME>>, <<CategoryId>>, <<Price>>)
                        VALUES (!!100, !!101, !!102), (!!103, !!104, !!105)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        INSERT INTO `dbo`.`Products` (`PROD_NAME`, `CategoryId`, `Price`)
                        VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES (:0, :1, :2), (:3, :4, :5)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES ($1, $2, $3), ($4, $5, $6)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES (@p1, @p2, @p3), (@p4, @p5, @p6)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        INSERT INTO [dbo].[Products] ([PROD_NAME], [CategoryId], [Price])
                        VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> ReturningSingleData
    {
        get
        {
            object?[] expectedParams = ["Test Product", 5, 19.99m];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES (@p0, @p1, @p2)
                        RETURNING "Id"
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES (:0, :1, :2)
                        RETURNING "Id"
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES ($1, $2, $3)
                        RETURNING "Id"
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES (@p1, @p2, @p3)
                        RETURNING "Id"
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        INSERT INTO [dbo].[Products] ([PROD_NAME], [CategoryId], [Price])
                        OUTPUT inserted.[Id]
                        VALUES (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> ReturningMultipleData
    {
        get
        {
            object?[] expectedParams = ["Test Product", 5, 19.99m];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES (@p0, @p1, @p2)
                        RETURNING "Id", "PROD_NAME"
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES (:0, :1, :2)
                        RETURNING "Id", "PROD_NAME"
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES ($1, $2, $3)
                        RETURNING "Id", "PROD_NAME"
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                        VALUES (@p1, @p2, @p3)
                        RETURNING "Id", "PROD_NAME"
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        INSERT INTO [dbo].[Products] ([PROD_NAME], [CategoryId], [Price])
                        OUTPUT inserted.[Id], inserted.[PROD_NAME]
                        VALUES (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlErrorTestCase> UnsupportedReturningData =>
    [
        new SqlErrorTestCase(SqlDialectKind.CustomDb, typeof(SqlDialectException), "'RETURNING' is not supported"),
        new SqlErrorTestCase(SqlDialectKind.MySql, typeof(SqlDialectException), "'RETURNING' is not supported")
    ];

    public static TheoryData<SqlTestCase> InsertWithIgnoreData
    {
        get
        {
            object?[] expectedParams = [1, "Gadget"];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        INSERT INTO <<dbo>>.<<Products>> (<<Id>>, <<PROD_NAME>>)
                        VALUES (!!100, !!101)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("Id", "PROD_NAME")
                        VALUES (@p0, @p1)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        INSERT INTO `dbo`.`Products` (`Id`, `PROD_NAME`)
                        VALUES (@p0, @p1)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("Id", "PROD_NAME")
                        VALUES (:0, :1)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("Id", "PROD_NAME")
                        VALUES ($1, $2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        INSERT INTO "dbo"."Products" ("Id", "PROD_NAME")
                        VALUES (@p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        INSERT INTO [dbo].[Products] ([Id], [PROD_NAME])
                        VALUES (@p0, @p1)
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> InsertComplexData
    {
        get
        {
            object?[] expectedParams = [42, "Mechanical Keyboard", 1, "Electronics"];

            return
            [
                new SqlTestCase
                (
                    SqlDialectKind.CustomDb,
                    [
                        """
                        INSERT INTO <<tbl_complex_products>>
                        (<<Id>>, <<Name>>, <<Status>>, <<Category>>)
                        VALUES (!!100, !!101, !!102, !!103);
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase
                (
                    SqlDialectKind.Firebird,
                    [
                        """
                        INSERT INTO "tbl_complex_products"
                        ("Id", "Name", "Status", "Category")
                        VALUES (@p0, @p1, @p2, @p3);
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase
                (
                    SqlDialectKind.MySql,
                    [
                        """
                        INSERT INTO `tbl_complex_products`
                        (`Id`, `Name`, `Status`, `Category`)
                        VALUES (@p0, @p1, @p2, @p3);
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase
                (
                    SqlDialectKind.Oracle,
                    [
                        """
                        INSERT INTO "tbl_complex_products"
                        ("Id", "Name", "Status", "Category")
                        VALUES (:0, :1, :2, :3);
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase
                (
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        INSERT INTO "tbl_complex_products"
                        ("Id", "Name", "Status", "Category")
                        VALUES ($1, $2, $3, $4);
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase
                (
                    SqlDialectKind.SqLite,
                    [
                        """
                        INSERT INTO "tbl_complex_products"
                        ("Id", "Name", "Status", "Category")
                        VALUES (@p1, @p2, @p3, @p4);
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase
                (
                    SqlDialectKind.SqlServer,
                    [
                        """
                        INSERT INTO [tbl_complex_products]
                        ([Id], [Name], [Status], [Category])
                        VALUES (@p0, @p1, @p2, @p3);
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> InsertTemplateData
    {
        get
        {
            object?[] expectedParams = [30, 1, "Alice"];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        INSERT INTO <<Users>>
                        (<<Age>>, <<Id>>, <<Name>>)
                        VALUES (!!100, !!101, !!102)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        INSERT INTO "Users"
                        ("Age", "Id", "Name")
                        VALUES (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        INSERT INTO `Users`
                        (`Age`, `Id`, `Name`)
                        VALUES (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        INSERT INTO "Users"
                        ("Age", "Id", "Name")
                        VALUES (:0, :1, :2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        INSERT INTO "Users"
                        ("Age", "Id", "Name")
                        VALUES ($1, $2, $3)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        INSERT INTO "Users"
                        ("Age", "Id", "Name")
                        VALUES (@p1, @p2, @p3)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        INSERT INTO [Users]
                        ([Age], [Id], [Name])
                        VALUES (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}