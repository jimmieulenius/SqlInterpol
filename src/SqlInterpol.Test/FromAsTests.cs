using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class FromAsTests
{
    [Theory]
    [MemberData(nameof(From_EntityManualAliasData))]
    public void From_EntityManualAlias(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}} AS p
            """))
            .Build();

        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(From_EntitySqlTableAttributeData))]
    public void From_EntitySqlTableAttribute(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}} AS prod
            """))
            .Build();

        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(From_LiteralTableAsEntityWithoutAttributeData))]
    public void From_LiteralTableAsEntityWithoutAttribute(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        var result = db.Query<OrderLine>(ol =>
            db.Append($$"""
            SELECT
                {{ol[x => x.OrderId]}}
            FROM ORDER_LINES AS {{ol}}
            """))
            .Build();

        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(From_LiteralTableAsExplicitAliasedEntityData))]
    public void From_LiteralTableAsExplicitAliasedEntity(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM products AS {{p.As("prod")}}
            """))
            .Build();

        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(From_EntityAsEntityWithSchemaData))]
    public void From_EntityAsEntityWithSchema(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}} AS {{p}}
            """))
            .Build();

        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> From_EntityManualAliasData =>
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

    public static TheoryData<SqlTestCase> From_EntitySqlTableAttributeData =>
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

    public static TheoryData<SqlTestCase> From_LiteralTableAsEntityWithoutAttributeData =>
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

    public static TheoryData<SqlTestCase> From_LiteralTableAsExplicitAliasedEntityData =>
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

    public static TheoryData<SqlTestCase> From_EntityAsEntityWithSchemaData =>
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