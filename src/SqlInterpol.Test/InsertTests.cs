using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class InsertTests
{
    [SqlTable("Orders", Schema = "dbo")]
    public record OrderModel
    {
        public int Id { get; init; }

        [SqlColumn("order_status")]
        public string Status { get; init; } = "";
        
        public decimal Total { get; init; }
    }

    [Theory]
    [MemberData(nameof(AllDialectsInsertData))]
    public void Insert_WithImplicitSyntax_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // An anonymous DTO representing the new product
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act - Using the shorthand implicit syntax (no VALUES keyword)
        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}}
            {{newProduct}}
            """)).Build();

        // Assert SQL
        var expectedSql = testCase.ExpectedSql[0].Replace("\r\n", "\n");
        var actualSql = result.Sql.Replace("\r\n", "\n");
        Assert.Equal(expectedSql, actualSql);

        // Assert Parameters
        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal("Test Product", result.Parameters.ElementAt(0).Value);
        Assert.Equal(5, result.Parameters.ElementAt(1).Value);
        Assert.Equal(19.99m, result.Parameters.ElementAt(2).Value);
    }

    [Theory]
    [MemberData(nameof(AllDialectsInsertData))]
    public void Insert_WithExplicitValuesKeyword_RendersCorrectlyAndSwallowsKeyword(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act - Using the explicit visual syntax (with VALUES keyword)
        // The AST Rewriter should intercept and swallow the VALUES keyword!
        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}}
            VALUES {{newProduct}}
            """)).Build();

        // Assert SQL - Should match the EXACT SAME expected SQL as the implicit test!
        // We trim start/end because the swallowed keyword leaves a newline/space, which is normal.
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);

        // Assert Parameters
        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal("Test Product", result.Parameters.ElementAt(0).Value);
        Assert.Equal(5, result.Parameters.ElementAt(1).Value);
        Assert.Equal(19.99m, result.Parameters.ElementAt(2).Value);
    }

    // The shared expected SQL data for both syntax styles!
    public static TheoryData<SqlTestCase> AllDialectsInsertData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                INSERT INTO <<dbo>>.<<Products>>
                (<<PROD_NAME>>, <<CategoryId>>, <<Price>>)
                VALUES (!!100, !!101, !!102)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Products`
                (`PROD_NAME`, `CategoryId`, `Price`)
                VALUES (@p0, @p1, @p2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                INSERT INTO "dbo"."Products"
                ("PROD_NAME", "CategoryId", "Price")
                VALUES (:0, :1, :2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                INSERT INTO "dbo"."Products"
                ("PROD_NAME", "CategoryId", "Price")
                VALUES ($1, $2, $3)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                INSERT INTO "dbo"."Products"
                ("PROD_NAME", "CategoryId", "Price")
                VALUES (?0, ?1, ?2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                INSERT INTO [dbo].[Products]
                ([PROD_NAME], [CategoryId], [Price])
                VALUES (@p0, @p1, @p2)
                """
            ]
        ),
    ];

    [Theory]
    [MemberData(nameof(InsertData))]
    public void Insert_WithDto_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newOrder = new { Status = "New", Total = 10.50m };
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($"{Sql.Insert(o, newOrder)}"))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        Assert.Equal("New", result.Parameters.ElementAt(0).Value);
        Assert.Equal(10.50m, result.Parameters.ElementAt(1).Value);
    }

    [Theory]
    [MemberData(nameof(InsertData))]
    public void Insert_WithExplicitSets_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($"{Sql.Insert(o,
                Sql.Set(o[x => x.Status], "New"),
                Sql.Set(o[x => x.Total], 10.50m)
            )}"))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    [Theory]
    [MemberData(nameof(InsertData))]
    public void Insert_PureManual_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var status = "Manual";
        var total = 100.00m;
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            INSERT INTO {{o}}
            ({{o[x => x.Status]}}, {{o[x => x.Total]}})
            VALUES ({{status}}, {{total}})
            """))
            .Build();

        // Assert
        // We expect the exact same SQL as the macro version!
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        
        // Verify parameters
        Assert.Equal(status, result.Parameters.ElementAt(0).Value);
        Assert.Equal(total, result.Parameters.ElementAt(1).Value);
    }

    public static TheoryData<SqlTestCase> InsertData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                INSERT INTO <<dbo>>.<<Orders>>
                (<<order_status>>, <<Total>>)
                VALUES (!!100, !!101)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Orders`
                (`order_status`, `Total`)
                VALUES (@p0, @p1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                INSERT INTO "dbo"."Orders"
                ("order_status", "Total")
                VALUES (:0, :1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                INSERT INTO "dbo"."Orders"
                ("order_status", "Total")
                VALUES ($1, $2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                INSERT INTO "dbo"."Orders"
                ("order_status", "Total")
                VALUES (?0, ?1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                INSERT INTO [dbo].[Orders]
                ([order_status], [Total])
                VALUES (@p0, @p1)
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(InsertValuesData))]
    public void InsertValues_ManualStatement_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var dto = new { Status = "Active", Total = 50.00m };
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            INSERT INTO {{o}} 
            {{Sql.InsertValues(o, dto)}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> InsertValuesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, [
                """
                INSERT INTO <<dbo>>.<<Orders>> 
                (<<order_status>>, <<Total>>)
                VALUES (!!100, !!101)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, [
                """
                INSERT INTO `dbo`.`Orders` 
                (`order_status`, `Total`)
                VALUES (@p0, @p1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, [
                """
                INSERT INTO "dbo"."Orders" 
                ("order_status", "Total")
                VALUES (:0, :1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, [
                """
                INSERT INTO "dbo"."Orders" 
                ("order_status", "Total")
                VALUES ($1, $2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, [
                """
                INSERT INTO "dbo"."Orders" 
                ("order_status", "Total")
                VALUES (?0, ?1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, [
                """
                INSERT INTO [dbo].[Orders] 
                ([order_status], [Total])
                VALUES (@p0, @p1)
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(AllDialectsBulkInsertData))]
    public void Insert_BulkImplicitSyntax_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Passing an array of anonymous objects
        var products = new[]
        {
            new { Name = "Prod1", CategoryId = 1, Price = 10m },
            new { Name = "Prod2", CategoryId = 2, Price = 20m }
        };

        // Act - Using the shorthand bulk syntax!
        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}} VALUES {{products}}
            """)).Build();

        // Assert SQL
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);

        // Assert Parameters - It should safely unpack all 6 values across 2 rows!
        Assert.Equal(6, result.Parameters.Count);
        Assert.Equal("Prod1", result.Parameters.ElementAt(0).Value);
        Assert.Equal(1, result.Parameters.ElementAt(1).Value);
        Assert.Equal(10m, result.Parameters.ElementAt(2).Value);
        
        Assert.Equal("Prod2", result.Parameters.ElementAt(3).Value);
        Assert.Equal(2, result.Parameters.ElementAt(4).Value);
        Assert.Equal(20m, result.Parameters.ElementAt(5).Value);
    }

    public static TheoryData<SqlTestCase> AllDialectsBulkInsertData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                INSERT INTO <<dbo>>.<<Products>> (<<PROD_NAME>>, <<CategoryId>>, <<Price>>)
                VALUES (!!100, !!101, !!102), (!!103, !!104, !!105)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Products` (`PROD_NAME`, `CategoryId`, `Price`)
                VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                VALUES (:0, :1, :2), (:3, :4, :5)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                VALUES ($1, $2, $3), ($4, $5, $6)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                VALUES (?0, ?1, ?2), (?3, ?4, ?5)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                INSERT INTO [dbo].[Products] ([PROD_NAME], [CategoryId], [Price])
                VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(AllDialectsReturningSingleData))]
    public void Insert_ReturningSingleColumn_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act - Requesting the auto-incremented ID back
        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}} {{newProduct}}
            RETURNING {{p[x => x.Id]}}
            """)).Build();

        // Assert SQL
        var expectedSql = testCase.ExpectedSql[0].Replace("\r\n", "\n").Trim();
        var actualSql = result.Sql.Replace("\r\n", "\n").Trim();
        
        Assert.Equal(expectedSql, actualSql);
    }

    public static TheoryData<SqlTestCase> AllDialectsReturningSingleData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                INSERT INTO <<dbo>>.<<Products>> (<<PROD_NAME>>, <<CategoryId>>, <<Price>>)
                VALUES (!!100, !!101, !!102)
                RETURNING <<Id>>
                """
            ]
        ),
        // MySQL (Pass-through: Note that MySQL doesn't natively support RETURNING, 
        // but this proves the engine leaves the AST untouched for non-SQL Server dialects)
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Products` (`PROD_NAME`, `CategoryId`, `Price`)
                VALUES (@p0, @p1, @p2)
                RETURNING `Id`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                VALUES (:0, :1, :2)
                RETURNING "Id"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                VALUES ($1, $2, $3)
                RETURNING "Id"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                VALUES (?0, ?1, ?2)
                RETURNING "Id"
                """
            ]
        ),
        // SQL Server (Intercepts RETURNING and injects OUTPUT inserted.[Id])
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                INSERT INTO [dbo].[Products] ([PROD_NAME], [CategoryId], [Price])
                OUTPUT inserted.[Id]
                VALUES (@p0, @p1, @p2)
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(AllDialectsReturningMultipleData))]
    public void Insert_ReturningMultipleColumns_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act - Requesting multiple columns back (Id and the mapped PROD_NAME)
        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}} {{newProduct}}
            RETURNING {{p[x => x.Id]}}, {{p[x => x.Name]}}
            """)).Build();

        // Assert SQL
        var expectedSql = testCase.ExpectedSql[0].Replace("\r\n", "\n").Trim();
        var actualSql = result.Sql.Replace("\r\n", "\n").Trim();
        
        Assert.Equal(expectedSql, actualSql);
    }

    public static TheoryData<SqlTestCase> AllDialectsReturningMultipleData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                INSERT INTO <<dbo>>.<<Products>> (<<PROD_NAME>>, <<CategoryId>>, <<Price>>)
                VALUES (!!100, !!101, !!102)
                RETURNING <<Id>>, <<PROD_NAME>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Products` (`PROD_NAME`, `CategoryId`, `Price`)
                VALUES (@p0, @p1, @p2)
                RETURNING `Id`, `PROD_NAME`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                VALUES (:0, :1, :2)
                RETURNING "Id", "PROD_NAME"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                VALUES ($1, $2, $3)
                RETURNING "Id", "PROD_NAME"
                """
        ]),

        // SQLite
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                INSERT INTO "dbo"."Products" ("PROD_NAME", "CategoryId", "Price")
                VALUES (?0, ?1, ?2)
                RETURNING "Id", "PROD_NAME"
                """
            ]),
        
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                INSERT INTO [dbo].[Products] ([PROD_NAME], [CategoryId], [Price])
                OUTPUT inserted.[Id], inserted.[PROD_NAME]
                VALUES (@p0, @p1, @p2)
                """
            ]
        )
    ];
}