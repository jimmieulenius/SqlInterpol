using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Tests;

public class BasicProjectionTests
{
    [Theory]
    [MemberData(nameof(AppendData))]
    public void Projection_Append(SqlBuilder db, string expected)
    {
        // Arrange
        var (_, p) = db.Entity<Product>();
        
        // Act
        db.Append($"SELECT {p[x => x.Id]} FROM {p}");
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> AppendData => new()
    {
        {
            SqlBuilder.CustomDb(),
            "SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>>" },
        {
            SqlBuilder.MySql(),
            "SELECT `dbo`.`Products`.`Id` FROM `dbo`.`Products`" },
        {
            SqlBuilder.Oracle(),
            "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\"" },
        {
            SqlBuilder.PostgreSql(),
            "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\"" },
        {
            SqlBuilder.SqLite(),
            "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\"" },
        {
            SqlBuilder.SqlServer(),
            "SELECT [dbo].[Products].[Id] FROM [dbo].[Products]" }
    };

    [Theory]
    [MemberData(nameof(AppendLineData))]
    public void Projection_AppendLine(SqlBuilder db, string expected)
    {
        // Arrange
        var (_, p) = db.Entity<Product>();
        
        // Act
        db.AppendLine($"SELECT {p[x => x.Id]}");
        db.Append($"FROM {p}");
        var result = db.Build();
    
        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> AppendLineData => new()
    {
        {
            SqlBuilder.CustomDb(),
            $"SELECT <<dbo>>.<<Products>>.<<Id>>{Environment.NewLine
            }FROM <<dbo>>.<<Products>>"
        },
        {
            SqlBuilder.MySql(),
            $"SELECT `dbo`.`Products`.`Id`{Environment.NewLine
            }FROM `dbo`.`Products`"
        },
        {
            SqlBuilder.Oracle(),
            $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine
            }FROM \"dbo\".\"Products\""
        },
        {
            SqlBuilder.PostgreSql(),
            $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine
            }FROM \"dbo\".\"Products\""
        },
        {
            SqlBuilder.SqLite(),
            $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine
            }FROM \"dbo\".\"Products\""
        },
        {
            SqlBuilder.SqlServer(),
            $"SELECT [dbo].[Products].[Id]{Environment.NewLine
            }FROM [dbo].[Products]"
        }
    };

    [Theory]
    [MemberData(nameof(RawStringData))]
    public void Projection_RawString(SqlBuilder db, string expected)
    {
        // Arrange
        var (_, p) = db.Entity<Product>();
        
        // Act
        db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}}
            """);
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> RawStringData => new()
    {
        { 
            SqlBuilder.CustomDb(), 
            """
            SELECT
                <<dbo>>.<<Products>>.<<Id>>
            FROM <<dbo>>.<<Products>>
            """ 
        },
        { 
            SqlBuilder.MySql(), 
            """
            SELECT
                `dbo`.`Products`.`Id`
            FROM `dbo`.`Products`
            """ 
        },
        { 
            SqlBuilder.Oracle(), 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            """ 
        },
        { 
            SqlBuilder.PostgreSql(), 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            """ 
        },
        { 
            SqlBuilder.SqLite(), 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            """ 
        },
        { 
            SqlBuilder.SqlServer(), 
            """
            SELECT
                [dbo].[Products].[Id]
            FROM [dbo].[Products]
            """ 
        }
    };
}