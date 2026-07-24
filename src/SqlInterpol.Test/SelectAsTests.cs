using SqlInterpol.Test.Models;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Schema;
using SqlInterpol.Configuration;

namespace SqlInterpol.Test;

public class SelectAsTests
{
    [Theory]
    [MemberData(nameof(ProjectionAsLiteralData))]
    public void SelectAs_LiteralProjection(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => db
            .Entity<Product>(out var p)
            .Append($$"""
            SELECT
                {{p.Id}} AS ProductId
            FROM {{p}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(RawColumnAsProjectionData))]
    public void SelectAs_RawColumnProjection(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => db
            .Entity<Product>(out var p)
            .Append($$"""
            SELECT
                {{p.Column("Id")}} AS {{"ProductId"}}
            FROM {{p}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(ProjectionAsProjectionWithAttributeData))]
    public void SelectAs_ProjectionWithAttribute(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => db
            .Entity<Product>(out var p)
            .Append($$"""
            SELECT
                {{p.Name}} AS {{p.Name}}
            FROM {{p}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> ProjectionAsLiteralData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<dbo>>.<<Products>>.<<Id>> AS <<ProductId>>
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "dbo"."Products"."Id" AS "ProductId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `dbo`.`Products`.`Id` AS `ProductId`
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "dbo"."Products"."Id" AS "ProductId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "dbo"."Products"."Id" AS "ProductId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "dbo"."Products"."Id" AS "ProductId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [dbo].[Products].[Id] AS [ProductId]
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> RawColumnAsProjectionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<dbo>>.<<Products>>.<<Id>> AS <<ProductId>>
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "dbo"."Products"."Id" AS "ProductId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `dbo`.`Products`.`Id` AS `ProductId`
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "dbo"."Products"."Id" AS "ProductId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "dbo"."Products"."Id" AS "ProductId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "dbo"."Products"."Id" AS "ProductId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [dbo].[Products].[Id] AS [ProductId]
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> ProjectionAsProjectionWithAttributeData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<dbo>>.<<Products>>.<<PROD_NAME>> AS <<Name>>
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "dbo"."Products"."PROD_NAME" AS "Name"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
                    `dbo`.`Products`.`PROD_NAME` AS `Name`
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "dbo"."Products"."PROD_NAME" AS "Name"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "dbo"."Products"."PROD_NAME" AS "Name"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "dbo"."Products"."PROD_NAME" AS "Name"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [dbo].[Products].[PROD_NAME] AS [Name]
                FROM [dbo].[Products]
                """
            ]
        )
    ];
}