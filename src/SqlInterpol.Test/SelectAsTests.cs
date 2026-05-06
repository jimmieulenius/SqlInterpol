using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class SelectAsTests
{
    [Theory]
    [MemberData(nameof(ProjectionAsLiteralData))]
    public void Select_Projection_As_Literal(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}} AS ProductId
            FROM {{p}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> ProjectionAsLiteralData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<dbo>>.<<Products>>.<<Id>> AS ProductId
            FROM <<dbo>>.<<Products>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `dbo`.`Products`.`Id` AS ProductId
            FROM `dbo`.`Products`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "dbo"."Products"."Id" AS ProductId
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "dbo"."Products"."Id" AS ProductId
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                "dbo"."Products"."Id" AS ProductId
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [dbo].[Products].[Id] AS ProductId
            FROM [dbo].[Products]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(RawColumnAsProjectionData))]
    public void Select_RawColumn_As_Projection(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p["ProductId"]}} AS {{p[x => x.Id]}}
            FROM {{p}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> RawColumnAsProjectionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<dbo>>.<<Products>>.<<ProductId>> AS <<Id>>
            FROM <<dbo>>.<<Products>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `dbo`.`Products`.`ProductId` AS `Id`
            FROM `dbo`.`Products`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "dbo"."Products"."ProductId" AS "Id"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "dbo"."Products"."ProductId" AS "Id"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                "dbo"."Products"."ProductId" AS "Id"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [dbo].[Products].[ProductId] AS [Id]
            FROM [dbo].[Products]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(ProjectionAsProjectionWithAttributeData))]
    public void Select_Projection_As_Projection_WithAttribute(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Name]}} AS {{p[x => x.Name]}}
            FROM {{p}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> ProjectionAsProjectionWithAttributeData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<dbo>>.<<Products>>.<<PROD_NAME>> AS <<Name>>
            FROM <<dbo>>.<<Products>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `dbo`.`Products`.`PROD_NAME` AS `Name`
            FROM `dbo`.`Products`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "dbo"."Products"."PROD_NAME" AS "Name"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "dbo"."Products"."PROD_NAME" AS "Name"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                "dbo"."Products"."PROD_NAME" AS "Name"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [dbo].[Products].[PROD_NAME] AS [Name]
            FROM [dbo].[Products]
            """
        )
    ];
}