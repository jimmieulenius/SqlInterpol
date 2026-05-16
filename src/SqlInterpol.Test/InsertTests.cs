using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class InsertTests
{
    [Theory]
    [MemberData(nameof(AllDialectsInsertData))]
    public void Insert_WithImplicitSyntax(SqlTestCase testCase)
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
        testCase.AssertSql(result.Sql);

        // Assert Parameters
        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal("Test Product", result.Parameters.ElementAt(0).Value);
        Assert.Equal(5, result.Parameters.ElementAt(1).Value);
        Assert.Equal(19.99m, result.Parameters.ElementAt(2).Value);
    }

    [Theory]
    [MemberData(nameof(AllDialectsInsertData))]
    public void Insert_WithExplicitValuesKeyword(SqlTestCase testCase)
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
        testCase.AssertSql(result.Sql);

        // Assert Parameters
        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal("Test Product", result.Parameters.ElementAt(0).Value);
        Assert.Equal(5, result.Parameters.ElementAt(1).Value);
        Assert.Equal(19.99m, result.Parameters.ElementAt(2).Value);
    }

    [Theory]
    [MemberData(nameof(InsertData))]
    public void Insert_PureManual(SqlTestCase testCase)
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
        testCase.AssertSql(result.Sql);
        
        // Verify parameters
        Assert.Equal(status, result.Parameters.ElementAt(0).Value);
        Assert.Equal(total, result.Parameters.ElementAt(1).Value);
    }

    [Theory]
    [MemberData(nameof(AllDialectsBulkInsertData))]
    public void Insert_BulkImplicitSyntax(SqlTestCase testCase)
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
        testCase.AssertSql(result.Sql);

        // Assert Parameters - It should safely unpack all 6 values across 2 rows!
        Assert.Equal(6, result.Parameters.Count);
        Assert.Equal("Prod1", result.Parameters.ElementAt(0).Value);
        Assert.Equal(1, result.Parameters.ElementAt(1).Value);
        Assert.Equal(10m, result.Parameters.ElementAt(2).Value);
        
        Assert.Equal("Prod2", result.Parameters.ElementAt(3).Value);
        Assert.Equal(2, result.Parameters.ElementAt(4).Value);
        Assert.Equal(20m, result.Parameters.ElementAt(5).Value);
    }

    [Theory]
    [MemberData(nameof(AllDialectsReturningSingleData))]
    public void Insert_ReturningSingleColumn(SqlTestCase testCase)
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
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(AllDialectsReturningMultipleData))]
    public void Insert_ReturningMultipleColumns(SqlTestCase testCase)
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
        testCase.AssertSql(result.Sql);
    }

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