using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class JoinAsTests
{
    [Theory]
    [MemberData(nameof(JoinWithLiteralAliasesData))]
    public void Join_WithLiteralAliases_ShouldAvoidDoubleAs(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<Product, OrderLine>((p, ol) =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}},
                {{ol[x => x.OrderId]}}
            FROM {{p}} AS p
            JOIN {{ol}} AS ol
                ON {{p[x => x.Id]}} = {{ol[x => x.ProductItemNumber]}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> JoinWithLiteralAliasesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<p>>.<<Id>>,
                <<ol>>.<<OrderId>>
            FROM <<dbo>>.<<Products>> AS p
            JOIN <<OrderLine>> AS ol
                ON <<p>>.<<Id>> = <<ol>>.<<ProductItemNumber>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `p`.`Id`,
                `ol`.`OrderId`
            FROM `dbo`.`Products` AS p
            JOIN `OrderLine` AS ol
                ON `p`.`Id` = `ol`.`ProductItemNumber`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "p"."Id",
                "ol"."OrderId"
            FROM "dbo"."Products" AS p
            JOIN "OrderLine" AS ol
                ON "p"."Id" = "ol"."ProductItemNumber"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "p"."Id",
                "ol"."OrderId"
            FROM "dbo"."Products" AS p
            JOIN "OrderLine" AS ol
                ON "p"."Id" = "ol"."ProductItemNumber"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                "p"."Id",
                "ol"."OrderId"
            FROM "dbo"."Products" AS p
            JOIN "OrderLine" AS ol
                ON "p"."Id" = "ol"."ProductItemNumber"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [p].[Id],
                [ol].[OrderId]
            FROM [dbo].[Products] AS p
            JOIN [OrderLine] AS ol
                ON [p].[Id] = [ol].[ProductItemNumber]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(JoinWithExplicitApiAliasesData))]
    public void Join_WithExplicitApiAliases_ShouldRenderCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<Product, OrderLine>((p, ol) =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}},
                {{ol[x => x.OrderId]}}
            FROM dbo.Products AS {{p.Alias("prod")}}
            JOIN order_lines AS {{ol}}
                ON {{p[x => x.Id]}} = {{ol[x => x.ProductItemNumber]}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> JoinWithExplicitApiAliasesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<prod>>.<<Id>>,
                <<OrderLine>>.<<OrderId>>
            FROM dbo.Products AS <<prod>>
            JOIN order_lines AS <<OrderLine>>
                ON <<prod>>.<<Id>> = <<OrderLine>>.<<ProductItemNumber>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `prod`.`Id`,
                `OrderLine`.`OrderId`
            FROM dbo.Products AS `prod`
            JOIN order_lines AS `OrderLine`
                ON `prod`.`Id` = `OrderLine`.`ProductItemNumber`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "prod"."Id",
                "OrderLine"."OrderId"
            FROM dbo.Products AS "prod"
            JOIN order_lines AS "OrderLine"
                ON "prod"."Id" = "OrderLine"."ProductItemNumber"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "prod"."Id",
                "OrderLine"."OrderId"
            FROM dbo.Products AS "prod"
            JOIN order_lines AS "OrderLine"
                ON "prod"."Id" = "OrderLine"."ProductItemNumber"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                "prod"."Id",
                "OrderLine"."OrderId"
            FROM dbo.Products AS "prod"
            JOIN order_lines AS "OrderLine"
                ON "prod"."Id" = "OrderLine"."ProductItemNumber"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [prod].[Id],
                [OrderLine].[OrderId]
            FROM dbo.Products AS [prod]
            JOIN order_lines AS [OrderLine]
                ON [prod].[Id] = [OrderLine].[ProductItemNumber]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(SelfJoinData))]
    public void Join_SelfJoin_ShouldIsolateAliases(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<Product, Product>((p1, p2) =>
            db.Append($$"""
            SELECT
                {{p1[x => x.Id]}},
                {{p2[x => x.Id]}}
            FROM {{p1}} AS original
            JOIN {{p2}} AS related
                ON {{p1[x => x.CategoryId]}} = {{p2[x => x.CategoryId]}}
            """));

        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> SelfJoinData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<original>>.<<Id>>,
                <<related>>.<<Id>>
            FROM <<dbo>>.<<Products>> AS original
            JOIN <<dbo>>.<<Products>> AS related
                ON <<original>>.<<CategoryId>> = <<related>>.<<CategoryId>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `original`.`Id`,
                `related`.`Id`
            FROM `dbo`.`Products` AS original
            JOIN `dbo`.`Products` AS related
                ON `original`.`CategoryId` = `related`.`CategoryId`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "original"."Id",
                "related"."Id"
            FROM "dbo"."Products" AS original
            JOIN "dbo"."Products" AS related
                ON "original"."CategoryId" = "related"."CategoryId"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "original"."Id",
                "related"."Id"
            FROM "dbo"."Products" AS original
            JOIN "dbo"."Products" AS related
                ON "original"."CategoryId" = "related"."CategoryId"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            """
            SELECT
                "original"."Id",
                "related"."Id"
            FROM "dbo"."Products" AS original
            JOIN "dbo"."Products" AS related
                ON "original"."CategoryId" = "related"."CategoryId"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [original].[Id],
                [related].[Id]
            FROM [dbo].[Products] AS original
            JOIN [dbo].[Products] AS related
                ON [original].[CategoryId] = [related].[CategoryId]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(JoinWithConfigOverrideData))]
    public void Join_WithSqlEntityConfig_ShouldOverrideBaseNames(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Entity<Product>(name: "Archive_Products", schema: "history")
        .Entity<OrderLine>()
        .Query((p, o) =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}},
                {{o[x => x.OrderId]}}
            FROM {{p}}
            JOIN {{o}}
                ON {{p[x => x.Id]}} = {{o[x => x.ProductItemNumber]}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> JoinWithConfigOverrideData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<history>>.<<Archive_Products>>.<<Id>>,
                <<OrderLine>>.<<OrderId>>
            FROM <<history>>.<<Archive_Products>>
            JOIN <<OrderLine>>
                ON <<history>>.<<Archive_Products>>.<<Id>> = <<OrderLine>>.<<ProductItemNumber>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `history`.`Archive_Products`.`Id`,
                `OrderLine`.`OrderId`
            FROM `history`.`Archive_Products`
            JOIN `OrderLine`
                ON `history`.`Archive_Products`.`Id` = `OrderLine`.`ProductItemNumber`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "history"."Archive_Products"."Id",
                "OrderLine"."OrderId"
            FROM "history"."Archive_Products"
            JOIN "OrderLine"
                ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "history"."Archive_Products"."Id",
                "OrderLine"."OrderId"
            FROM "history"."Archive_Products"
            JOIN "OrderLine"
                ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            """
            SELECT
                "history"."Archive_Products"."Id",
                "OrderLine"."OrderId"
            FROM "history"."Archive_Products"
            JOIN "OrderLine"
                ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [history].[Archive_Products].[Id],
                [OrderLine].[OrderId]
            FROM [history].[Archive_Products]
            JOIN [OrderLine]
                ON [history].[Archive_Products].[Id] = [OrderLine].[ProductItemNumber]
            """
        )
    ];
}