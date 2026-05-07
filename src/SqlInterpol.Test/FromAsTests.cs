using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class FromAsTests
{
    [Theory]
    [MemberData(nameof(FromEntityWithManualAliasData))]
    public void From_Entity_WithManualAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}} AS p
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> FromEntityWithManualAliasData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<p>>.<<Id>>
                FROM <<dbo>>.<<Products>> AS p
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `p`.`Id`
                FROM `dbo`.`Products` AS p
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" AS p
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" AS p
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" AS p
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            [
                """
                SELECT
                    [p].[Id]
                FROM [dbo].[Products] AS p
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(FromEntityWithSqlTableAttributeData))]
    public void From_Entity_WithSqlTableAttribute(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}} AS prod
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> FromEntityWithSqlTableAttributeData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<prod>>.<<Id>>
                FROM <<dbo>>.<<Products>> AS prod
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `prod`.`Id`
                FROM `dbo`.`Products` AS prod
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" AS prod
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" AS prod
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" AS prod
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            [
                """
                SELECT
                    [prod].[Id]
                FROM [dbo].[Products] AS prod
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(FromLiteralAsEntityWithoutAttributeData))]
    public void From_LiteralTable_As_Entity_WithoutSqlTableAttribute(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<OrderLine>(ol =>
            db.Append($$"""
            SELECT
                {{ol[x => x.OrderId]}}
            FROM ORDER_LINES AS {{ol}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> FromLiteralAsEntityWithoutAttributeData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT
                    <<OrderLine>>.<<OrderId>>
                FROM ORDER_LINES AS <<OrderLine>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
                    `OrderLine`.`OrderId`
                FROM ORDER_LINES AS `OrderLine`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "OrderLine"."OrderId"
                FROM ORDER_LINES AS "OrderLine"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "OrderLine"."OrderId"
                FROM ORDER_LINES AS "OrderLine"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "OrderLine"."OrderId"
                FROM ORDER_LINES AS "OrderLine"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [OrderLine].[OrderId]
                FROM ORDER_LINES AS [OrderLine]
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(FromLiteralAsExplicitAliasedEntityData))]
    public void From_LiteralTable_As_ExplicitAliasedEntity(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM products AS {{p.Alias("prod")}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> FromLiteralAsExplicitAliasedEntityData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT
                    <<prod>>.<<Id>>
                FROM products AS <<prod>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
                    `prod`.`Id`
                FROM products AS `prod`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT
                    "prod"."Id"
                FROM products AS "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "prod"."Id"
                FROM products AS "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "prod"."Id"
                FROM products AS "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            [
                """
                SELECT
                    [prod].[Id]
                FROM products AS [prod]
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(FromEntityAsEntityWithSchemaData))]
    public void From_Entity_As_Entity_WithSchema_ShouldUseTypeNameAsAliasFallback(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}} AS {{p}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> FromEntityAsEntityWithSchemaData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT
                    <<Product>>.<<Id>>
                FROM <<dbo>>.<<Products>> AS <<Product>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
                    `Product`.`Id`
                FROM `dbo`.`Products` AS `Product`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT
                    "Product"."Id"
                FROM "dbo"."Products" AS "Product"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "Product"."Id"
                FROM "dbo"."Products" AS "Product"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "Product"."Id"
                FROM "dbo"."Products" AS "Product"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            [
                """
                SELECT
                    [Product].[Id]
                FROM [dbo].[Products] AS [Product]
                """
            ]
        )
    ];
}