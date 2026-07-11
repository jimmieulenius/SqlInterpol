using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class FromAsTests
{
    [Theory]
    [MemberData(nameof(From_EntityManualAliasData))]
    public void From_EntityManualAlias(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();

            return db
                .Entity<Product>(out var p)
                .Append($$"""
                    SELECT
                        {{p.Id}}
                    FROM {{p}} AS p
                    """)
                .Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(From_EntitySqlTableAttributeData))]
    public void From_EntitySqlTableAttribute(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            
            return db
                .Entity<Product>(out var p)
                .Append($$"""
                    SELECT
                        {{p.Id}}
                    FROM {{p}} AS prod
                    """)
                .Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(From_LiteralTableAsEntityWithoutAttributeData))]
    public void From_LiteralTableAsEntityWithoutAttribute(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();

            return db
                .Entity<OrderLine>(out var ol)
                .Append($$"""
                    SELECT
                        {{ol.OrderId}}
                    FROM ORDER_LINES AS {{ol:alias}}
                    """)
                .Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(From_LiteralTableAsExplicitAliasedEntityData))]
    public void From_LiteralTableAsExplicitAliasedEntity(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();

            return db
                .Entity<Product>(out var p, "prod")
                .Append($$"""
                    SELECT
                        {{p.Id}}
                    FROM products AS {{p:alias}}
                    """)
                .Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(From_EntityAsEntityWithSchemaData))]
    public void From_EntityAsEntityWithSchema(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();

            return db
                .Entity<Product>(out var p, "Product")
                .Append($$"""
                    SELECT
                        {{p.Id}}
                    FROM {{p:base}} AS {{p:alias}}
                    """)
                .Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(FromAsEntityAsItsOwnAliasInceptionData))]
    public void From_As_EntityAsItsOwnAlias_Inception(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();

            return db
                .Entity<Product>(out var p)
                .Append($$"""
                    SELECT {{p}}
                    FROM {{p:base}} AS {{p:alias}}
                    """)
                .Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(From_EntityAutoAliasingData))]
    public void From_EntityAutoAliasing(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            db.Context.Options.EntityAutoAliasing = true;

            return db
                .Entity<Product>(out var prod)
                .Append($$"""
                    SELECT
                        {{prod.Id}}
                    FROM {{prod}}
                    """)
                .Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(From_EntityAutoAliasingManualOverrideData))]
    public void From_EntityAutoAliasing_ManualOverride(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            db.Context.Options.EntityAutoAliasing = true;

            return db
                .Entity<Product>(out var prod)
                .Append($$"""
                    SELECT
                        {{prod.Id}}
                    FROM {{prod}} AS p
                    """)
                .Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(From_AutoAliasingInceptionData))]
    public void From_AutoAliasing_Inception(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            db.Context.Options.EntityAutoAliasing = true;

            return db
                .Entity<Product>(out var myProd)
                .Append($$"""
                    SELECT {{myProd}}
                    FROM {{myProd}}
                    """)
                .Build();
        });

        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> From_EntityManualAliasData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<p>>.<<Id>>
                FROM <<dbo>>.<<Products>> AS <<p>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" AS "p"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `p`.`Id`
                FROM `dbo`.`Products` AS `p`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" "p"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" AS "p"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" AS "p"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [p].[Id]
                FROM [dbo].[Products] AS [p]
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
                FROM <<dbo>>.<<Products>> AS <<prod>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" AS "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `prod`.`Id`
                FROM `dbo`.`Products` AS `prod`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" AS "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" AS "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [prod].[Id]
                FROM [dbo].[Products] AS [prod]
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
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "OrderLine"."OrderId"
                FROM ORDER_LINES AS "OrderLine"
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
                FROM ORDER_LINES "OrderLine"
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
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "prod"."Id"
                FROM products AS "prod"
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
                FROM products "prod"
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
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "Product"."Id"
                FROM "dbo"."Products" AS "Product"
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
                FROM "dbo"."Products" "Product"
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

    public static TheoryData<SqlTestCase> FromAsEntityAsItsOwnAliasInceptionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<Product>>.<<CategoryId>>, <<Product>>.<<Id>>, <<Product>>.<<IsActive>>, <<Product>>.<<PROD_NAME>>, <<Product>>.<<Price>>
                FROM <<dbo>>.<<Products>> AS <<Product>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT "Product"."CategoryId", "Product"."Id", "Product"."IsActive", "Product"."PROD_NAME", "Product"."Price"
                FROM "dbo"."Products" AS "Product"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `Product`.`CategoryId`, `Product`.`Id`, `Product`.`IsActive`, `Product`.`PROD_NAME`, `Product`.`Price`
                FROM `dbo`.`Products` AS `Product`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "Product"."CategoryId", "Product"."Id", "Product"."IsActive", "Product"."PROD_NAME", "Product"."Price"
                FROM "dbo"."Products" "Product"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "Product"."CategoryId", "Product"."Id", "Product"."IsActive", "Product"."PROD_NAME", "Product"."Price"
                FROM "dbo"."Products" AS "Product"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "Product"."CategoryId", "Product"."Id", "Product"."IsActive", "Product"."PROD_NAME", "Product"."Price"
                FROM "dbo"."Products" AS "Product"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [Product].[CategoryId], [Product].[Id], [Product].[IsActive], [Product].[PROD_NAME], [Product].[Price]
                FROM [dbo].[Products] AS [Product]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> From_EntityAutoAliasingData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<prod>>.<<Id>>
                FROM <<dbo>>.<<Products>> AS <<prod>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" AS "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `prod`.`Id`
                FROM `dbo`.`Products` AS `prod`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" AS "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "prod"."Id"
                FROM "dbo"."Products" AS "prod"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [prod].[Id]
                FROM [dbo].[Products] AS [prod]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> From_EntityAutoAliasingManualOverrideData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<p>>.<<Id>>
                FROM <<dbo>>.<<Products>> AS <<p>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" AS "p"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `p`.`Id`
                FROM `dbo`.`Products` AS `p`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" "p"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" AS "p"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "p"."Id"
                FROM "dbo"."Products" AS "p"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [p].[Id]
                FROM [dbo].[Products] AS [p]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> From_AutoAliasingInceptionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<myProd>>.<<CategoryId>>, <<myProd>>.<<Id>>, <<myProd>>.<<IsActive>>, <<myProd>>.<<PROD_NAME>>, <<myProd>>.<<Price>>
                FROM <<dbo>>.<<Products>> AS <<myProd>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT "myProd"."CategoryId", "myProd"."Id", "myProd"."IsActive", "myProd"."PROD_NAME", "myProd"."Price"
                FROM "dbo"."Products" AS "myProd"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `myProd`.`CategoryId`, `myProd`.`Id`, `myProd`.`IsActive`, `myProd`.`PROD_NAME`, `myProd`.`Price`
                FROM `dbo`.`Products` AS `myProd`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "myProd"."CategoryId", "myProd"."Id", "myProd"."IsActive", "myProd"."PROD_NAME", "myProd"."Price"
                FROM "dbo"."Products" "myProd"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "myProd"."CategoryId", "myProd"."Id", "myProd"."IsActive", "myProd"."PROD_NAME", "myProd"."Price"
                FROM "dbo"."Products" AS "myProd"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "myProd"."CategoryId", "myProd"."Id", "myProd"."IsActive", "myProd"."PROD_NAME", "myProd"."Price"
                FROM "dbo"."Products" AS "myProd"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [myProd].[CategoryId], [myProd].[Id], [myProd].[IsActive], [myProd].[PROD_NAME], [myProd].[Price]
                FROM [dbo].[Products] AS [myProd]
                """
            ]
        )
    ];
}