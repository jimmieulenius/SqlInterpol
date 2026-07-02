using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class JoinAsTests
{
    [Theory]
    [MemberData(nameof(JoinWithLiteralAliasesData))]
    public void Join_WithLiteralAliases(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Entity<OrderLine>(out var ol)
            .Append($$"""
            SELECT
                {{p.Id}},
                {{ol.OrderId}}
            FROM {{p}} AS p
            JOIN {{ol}} AS ol
                ON {{p.Id}} = {{ol.ProductItemNumber}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(JoinWithExplicitApiAliasesData))]
    public void Join_WithExplicitApiAliases(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        // We pass the explicit alias into the Entity declaration, and then use the 
        // :alias format specifier to render only the quoted alias in the FROM clause!
        testCase.Action(() => db.Entity<Product>(out var prod, "prod")
            .Entity<OrderLine>(out var OrderLine, "OrderLine")
            .Append($$"""
            SELECT
                {{prod.Id}},
                {{OrderLine.OrderId}}
            FROM dbo.Products AS {{prod:alias}}
            JOIN order_lines AS {{OrderLine:alias}}
                ON {{prod.Id}} = {{OrderLine.ProductItemNumber}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(SelfJoinData))]
    public void Join_SelfJoin(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        // The preprocessor naturally detects "AS original" and "AS related" 
        // and wires them automatically to the p1 and p2 scopes!
        testCase.Action(() => db.Entity<Product>(out var p1)
            .Entity<Product>(out var p2)
            .Append($$"""
            SELECT
                {{p1.Id}},
                {{p2.Id}}
            FROM {{p1}} AS original
            JOIN {{p2}} AS related
                ON {{p1.CategoryId}} = {{p2.CategoryId}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(JoinWithConfigOverrideData))]
    public void Join_WithSqlEntityConfig(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        // Brilliant API experience! The metadata overrides happen securely at declaration time!
        testCase.Action(() => db.Entity<Product>(out var p, name: "Archive_Products", schema: "history")
            .Entity<OrderLine>(out var ol)
            .Append($$"""
            SELECT
                {{p.Id}},
                {{ol.OrderId}}
            FROM {{p}}
            JOIN {{ol}}
                ON {{p.Id}} = {{ol.ProductItemNumber}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> JoinWithLiteralAliasesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<p>>.<<Id>>,
                    <<ol>>.<<OrderId>>
                FROM <<dbo>>.<<Products>> AS <<p>>
                JOIN <<OrderLine>> AS <<ol>>
                    ON <<p>>.<<Id>> = <<ol>>.<<ProductItemNumber>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "p"."Id",
                    "ol"."OrderId"
                FROM "dbo"."Products" AS "p"
                JOIN "OrderLine" AS "ol"
                    ON "p"."Id" = "ol"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `p`.`Id`,
                    `ol`.`OrderId`
                FROM `dbo`.`Products` AS `p`
                JOIN `OrderLine` AS `ol`
                    ON `p`.`Id` = `ol`.`ProductItemNumber`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "p"."Id",
                    "ol"."OrderId"
                FROM "dbo"."Products" "p"
                JOIN "OrderLine" "ol"
                    ON "p"."Id" = "ol"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "p"."Id",
                    "ol"."OrderId"
                FROM "dbo"."Products" AS "p"
                JOIN "OrderLine" AS "ol"
                    ON "p"."Id" = "ol"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "p"."Id",
                    "ol"."OrderId"
                FROM "dbo"."Products" AS "p"
                JOIN "OrderLine" AS "ol"
                    ON "p"."Id" = "ol"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [p].[Id],
                    [ol].[OrderId]
                FROM [dbo].[Products] AS [p]
                JOIN [OrderLine] AS [ol]
                    ON [p].[Id] = [ol].[ProductItemNumber]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> JoinWithExplicitApiAliasesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT
                    <<prod>>.<<Id>>,
                    <<OrderLine>>.<<OrderId>>
                FROM dbo.Products AS <<prod>>
                JOIN order_lines AS <<OrderLine>>
                    ON <<prod>>.<<Id>> = <<OrderLine>>.<<ProductItemNumber>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "prod"."Id",
                    "OrderLine"."OrderId"
                FROM dbo.Products AS "prod"
                JOIN order_lines AS "OrderLine"
                    ON "prod"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `prod`.`Id`,
                    `OrderLine`.`OrderId`
                FROM dbo.Products AS `prod`
                JOIN order_lines AS `OrderLine`
                    ON `prod`.`Id` = `OrderLine`.`ProductItemNumber`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "prod"."Id",
                    "OrderLine"."OrderId"
                FROM dbo.Products "prod"
                JOIN order_lines "OrderLine"
                    ON "prod"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "prod"."Id",
                    "OrderLine"."OrderId"
                FROM dbo.Products AS "prod"
                JOIN order_lines AS "OrderLine"
                    ON "prod"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "prod"."Id",
                    "OrderLine"."OrderId"
                FROM dbo.Products AS "prod"
                JOIN order_lines AS "OrderLine"
                    ON "prod"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [prod].[Id],
                    [OrderLine].[OrderId]
                FROM dbo.Products AS [prod]
                JOIN order_lines AS [OrderLine]
                    ON [prod].[Id] = [OrderLine].[ProductItemNumber]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> SelfJoinData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT
                    <<original>>.<<Id>>,
                    <<related>>.<<Id>>
                FROM <<dbo>>.<<Products>> AS <<original>>
                JOIN <<dbo>>.<<Products>> AS <<related>>
                    ON <<original>>.<<CategoryId>> = <<related>>.<<CategoryId>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "original"."Id",
                    "related"."Id"
                FROM "dbo"."Products" AS "original"
                JOIN "dbo"."Products" AS "related"
                    ON "original"."CategoryId" = "related"."CategoryId"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `original`.`Id`,
                    `related`.`Id`
                FROM `dbo`.`Products` AS `original`
                JOIN `dbo`.`Products` AS `related`
                    ON `original`.`CategoryId` = `related`.`CategoryId`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "original"."Id",
                    "related"."Id"
                FROM "dbo"."Products" "original"
                JOIN "dbo"."Products" "related"
                    ON "original"."CategoryId" = "related"."CategoryId"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "original"."Id",
                    "related"."Id"
                FROM "dbo"."Products" AS "original"
                JOIN "dbo"."Products" AS "related"
                    ON "original"."CategoryId" = "related"."CategoryId"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            [
                """
                SELECT
                    "original"."Id",
                    "related"."Id"
                FROM "dbo"."Products" AS "original"
                JOIN "dbo"."Products" AS "related"
                    ON "original"."CategoryId" = "related"."CategoryId"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [original].[Id],
                    [related].[Id]
                FROM [dbo].[Products] AS [original]
                JOIN [dbo].[Products] AS [related]
                    ON [original].[CategoryId] = [related].[CategoryId]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> JoinWithConfigOverrideData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<history>>.<<Archive_Products>>.<<Id>>,
                    <<OrderLine>>.<<OrderId>>
                FROM <<history>>.<<Archive_Products>>
                JOIN <<OrderLine>>
                    ON <<history>>.<<Archive_Products>>.<<Id>> = <<OrderLine>>.<<ProductItemNumber>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "history"."Archive_Products"."Id",
                    "OrderLine"."OrderId"
                FROM "history"."Archive_Products"
                JOIN "OrderLine"
                    ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
                    `history`.`Archive_Products`.`Id`,
                    `OrderLine`.`OrderId`
                FROM `history`.`Archive_Products`
                JOIN `OrderLine`
                    ON `history`.`Archive_Products`.`Id` = `OrderLine`.`ProductItemNumber`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "history"."Archive_Products"."Id",
                    "OrderLine"."OrderId"
                FROM "history"."Archive_Products"
                JOIN "OrderLine"
                    ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "history"."Archive_Products"."Id",
                    "OrderLine"."OrderId"
                FROM "history"."Archive_Products"
                JOIN "OrderLine"
                    ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            [
                """
                SELECT
                    "history"."Archive_Products"."Id",
                    "OrderLine"."OrderId"
                FROM "history"."Archive_Products"
                JOIN "OrderLine"
                    ON "history"."Archive_Products"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [history].[Archive_Products].[Id],
                    [OrderLine].[OrderId]
                FROM [history].[Archive_Products]
                JOIN [OrderLine]
                    ON [history].[Archive_Products].[Id] = [OrderLine].[ProductItemNumber]
                """
            ]
        )
    ];
}