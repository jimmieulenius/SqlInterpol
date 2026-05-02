using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Tests;

public class BasicProjectionTests
{
    [Theory]
    [MemberData(nameof(AppendData))]
    public void Projection_Append(string _, SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>((p) =>
            db.Append($"SELECT {p[x => x.Id]} FROM {p}"));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<string, SqlBuilder, string> AppendData => new()
    {
        {
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(),
            "SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>>" },
        {
            SqlDialectKind.MySql,
            SqlBuilder.MySql(),
            "SELECT `dbo`.`Products`.`Id` FROM `dbo`.`Products`" },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(),
            "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\"" },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(),
            "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\"" },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(),
            "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\"" },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(),
            "SELECT [dbo].[Products].[Id] FROM [dbo].[Products]" }
    };

    [Theory]
    [MemberData(nameof(AppendLineData))]
    public void Projection_AppendLine(string _,SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>((p) =>
            db.AppendLine($"SELECT {p[x => x.Id]}")
            .Append($"FROM {p}"));
        
        // Act
        var result = db.Build();
    
        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<string, SqlBuilder, string> AppendLineData => new()
    {
        {
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(),
            $"SELECT <<dbo>>.<<Products>>.<<Id>>{Environment.NewLine
            }FROM <<dbo>>.<<Products>>"
        },
        {
            SqlDialectKind.MySql,
            SqlBuilder.MySql(),
            $"SELECT `dbo`.`Products`.`Id`{Environment.NewLine
            }FROM `dbo`.`Products`"
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(),
            $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine
            }FROM \"dbo\".\"Products\""
        },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(),
            $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine
            }FROM \"dbo\".\"Products\""
        },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(),
            $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine
            }FROM \"dbo\".\"Products\""
        },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(),
            $"SELECT [dbo].[Products].[Id]{Environment.NewLine
            }FROM [dbo].[Products]"
        }
    };

    [Theory]
    [MemberData(nameof(RawStringData))]
    public void Projection_RawString(string _, SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<string, SqlBuilder, string> RawStringData => new()
    {
        { 
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(), 
            """
            SELECT
                <<dbo>>.<<Products>>.<<Id>>
            FROM <<dbo>>.<<Products>>
            """ 
        },
        { 
            SqlDialectKind.MySql,
            SqlBuilder.MySql(), 
            """
            SELECT
                `dbo`.`Products`.`Id`
            FROM `dbo`.`Products`
            """ 
        },
        { 
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(), 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            """ 
        },
        { 
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(), 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            """ 
        },
        { 
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(), 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            """ 
        },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(), 
            """
            SELECT
                [dbo].[Products].[Id]
            FROM [dbo].[Products]
            """ 
        }
    };
}