using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class SqlBuilderTests
{
    [Theory]
    [MemberData(nameof(AppendData))]
    public void Projection_Append(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<Product>((p) =>
            db.Append($"SELECT {p[x => x.Id]}").Append($" FROM {p}"))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> AppendData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            "SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>>"
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            "SELECT `dbo`.`Products`.`Id` FROM `dbo`.`Products`"
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\""
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\""
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\""
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            "SELECT [dbo].[Products].[Id] FROM [dbo].[Products]"
        )
    ];

    [Theory]
    [MemberData(nameof(AppendLineData))]
    public void Projection_AppendLine(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<Product>((p) =>
            db.AppendLine($"SELECT {p[x => x.Id]}")
            .Append($"FROM {p}"))
            .Build();
    
        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> AppendLineData => new()
    {
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            $"SELECT <<dbo>>.<<Products>>.<<Id>>{Environment.NewLine}FROM <<dbo>>.<<Products>>"
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            $"SELECT `dbo`.`Products`.`Id`{Environment.NewLine}FROM `dbo`.`Products`"
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine}FROM \"dbo\".\"Products\""
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine}FROM \"dbo\".\"Products\""
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine}FROM \"dbo\".\"Products\""
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            $"SELECT [dbo].[Products].[Id]{Environment.NewLine}FROM [dbo].[Products]"
        )
    };

    [Theory]
    [MemberData(nameof(RawStringData))]
    public void Projection_RawString(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}}
            """)).Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> RawStringData =>
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
}
