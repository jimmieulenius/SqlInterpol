using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class SelectTests
{
    [Theory]
    [MemberData(nameof(SingleColumnData))]
    public void Select_SingleColumn(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> SingleColumnData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<dbo>>.<<Products>>.<<Id>>
            FROM <<dbo>>.<<Products>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `dbo`.`Products`.`Id`
            FROM `dbo`.`Products`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [dbo].[Products].[Id]
            FROM [dbo].[Products]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(MultipleColumnsData))]
    public void Select_MultipleColumns(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}},
                {{p[x => x.CategoryId]}}
            FROM {{p}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> MultipleColumnsData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<dbo>>.<<Products>>.<<Id>>,
                <<dbo>>.<<Products>>.<<CategoryId>>
            FROM <<dbo>>.<<Products>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `dbo`.`Products`.`Id`,
                `dbo`.`Products`.`CategoryId`
            FROM `dbo`.`Products`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "dbo"."Products"."Id",
                "dbo"."Products"."CategoryId"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "dbo"."Products"."Id",
                "dbo"."Products"."CategoryId"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                "dbo"."Products"."Id",
                "dbo"."Products"."CategoryId"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [dbo].[Products].[Id],
                [dbo].[Products].[CategoryId]
            FROM [dbo].[Products]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(SqlFunctionData))]
    public void Select_SqlFunction(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                COUNT({{p[x => x.Id]}})
            FROM {{p}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> SqlFunctionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                COUNT(<<dbo>>.<<Products>>.<<Id>>)
            FROM <<dbo>>.<<Products>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                COUNT(`dbo`.`Products`.`Id`)
            FROM `dbo`.`Products`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                COUNT("dbo"."Products"."Id")
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                COUNT("dbo"."Products"."Id")
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                COUNT("dbo"."Products"."Id")
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                COUNT([dbo].[Products].[Id])
            FROM [dbo].[Products]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(LiteralParameterData))]
    public void Select_LiteralParameter(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int activeStatus = 1;
        
        db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{activeStatus}}
            FROM {{p}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
        
        // Verify the parameter was captured correctly
        Assert.Single(result.Parameters);
        Assert.Equal(1, result.Parameters.Values.First());
    }

    public static TheoryData<SqlTestCase> LiteralParameterData =>
    [
        // Note: Replace the parameter prefix (@, :, etc.) based on your dialect's default setup
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                !!100
            FROM <<dbo>>.<<Products>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                @p0
            FROM `dbo`.`Products`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                :0
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                $1
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                ?0
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                @p0
            FROM [dbo].[Products]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(CustomColumnAttributeData))]
    public void Select_CustomColumnAttribute(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Name]}}
            FROM {{p}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> CustomColumnAttributeData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            $$"""
            SELECT
                <<dbo>>.<<Products>>.<<PROD_NAME>>
            FROM <<dbo>>.<<Products>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `dbo`.`Products`.`PROD_NAME`
            FROM `dbo`.`Products`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "dbo"."Products"."PROD_NAME"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "dbo"."Products"."PROD_NAME"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            """
            SELECT
                "dbo"."Products"."PROD_NAME"
            FROM "dbo"."Products"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [dbo].[Products].[PROD_NAME]
            FROM [dbo].[Products]
            """
        )
    ];
}