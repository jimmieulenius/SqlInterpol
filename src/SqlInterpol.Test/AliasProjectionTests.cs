using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Tests;

public class AliasProjectionTests
{
    [Theory]
    [MemberData(nameof(AppendAliasData))]
    public void Alias_Append(SqlBuilder db, string expected)
    {
        var (_, p) = db.Entities<Product>();
        
        db.Append($"SELECT {p[x => x.Id]} AS ProductId FROM {p} AS prd");
        
        var result = db.Build();
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> AppendAliasData => new()
    {
        { SqlBuilder.CustomDb(), "SELECT <<prd>>.<<Id>> AS ProductId FROM <<dbo>>.<<Products>> AS prd" },
        { SqlBuilder.MySql(), "SELECT `prd`.`Id` AS ProductId FROM `dbo`.`Products` AS prd" },
        { SqlBuilder.Oracle(), "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd" },
        { SqlBuilder.PostgreSql(), "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd" },
        { SqlBuilder.SqLite(), "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd" },
        { SqlBuilder.SqlServer(), "SELECT [prd].[Id] AS ProductId FROM [dbo].[Products] AS prd" }
    };

    [Theory]
    [MemberData(nameof(AppendQuotedAliasData))]
    public void QuotedAlias_Append(SqlBuilder db, string expected)
    {
        var (_, p) = db.Entities<Product>();
        var open = Sql.Raw(db.Dialect.OpenQuote);
        var close = Sql.Raw(db.Dialect.CloseQuote);
        
        db.Append($"SELECT {p[x => x.Id]} AS {open}ProductId{close} FROM {p} AS {open}prd{close}");
        
        var result = db.Build();
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> AppendQuotedAliasData => new()
    {
        {
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<Id>> AS ProductId FROM <<dbo>>.<<Products>> AS prd"
        },
        {
            SqlBuilder.MySql(),
            "SELECT `prd`.`Id` AS ProductId FROM `dbo`.`Products` AS prd"
        },
        {
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlBuilder.SqlServer(),
            "SELECT [prd].[Id] AS ProductId FROM [dbo].[Products] AS prd"
        }
    };

    [Theory]
    [MemberData(nameof(AppendLineAliasData))]
    public void Alias_AppendLine(SqlBuilder db, string expected)
    {
        var (_, p) = db.Entities<Product>();
        
        // Column is on line 1, Alias is discovered on line 2
        db.AppendLine($"SELECT {p[x => x.Id]} AS ProductId");
        db.Append($"FROM {p} AS prd");
        
        var result = db.Build();
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> AppendLineAliasData => new()
    {
        { 
            SqlBuilder.CustomDb(), 
            $"SELECT <<prd>>.<<Id>> AS ProductId{Environment.NewLine
            }FROM <<dbo>>.<<Products>> AS prd" 
        },
        { 
            SqlBuilder.MySql(), 
            $"SELECT `prd`.`Id` AS ProductId{Environment.NewLine
            }FROM `dbo`.`Products` AS prd" 
        },
        { 
            SqlBuilder.Oracle(), 
            $"SELECT \"prd\".\"Id\" AS ProductId{Environment.NewLine
            }FROM \"dbo\".\"Products\" AS prd" 
        },
        { 
            SqlBuilder.PostgreSql(), 
            $"SELECT \"prd\".\"Id\" AS ProductId{Environment.NewLine
            }FROM \"dbo\".\"Products\" AS prd" 
        },
        { 
            SqlBuilder.SqLite(), 
            $"SELECT \"prd\".\"Id\" AS ProductId{Environment.NewLine
            }FROM \"dbo\".\"Products\" AS prd" 
        },
        { 
            SqlBuilder.SqlServer(), 
            $"SELECT [prd].[Id] AS ProductId{Environment.NewLine
            }FROM [dbo].[Products] AS prd" 
        }
    };

    [Theory]
    [MemberData(nameof(RawStringAliasData))]
    public void Alias_RawString(SqlBuilder db, string expected)
    {
        var (_, p) = db.Entities<Product>();
        
        db.Append($$"""
            SELECT 
                {{p[x => x.Id]}} AS ProductId,
                {{p[x => x.Name]}} AS ProductName
            FROM {{p}} AS prd
            """);
        
        var result = db.Build();
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> RawStringAliasData => new()
    {
        { 
            SqlBuilder.CustomDb(), 
            """
            SELECT 
                <<prd>>.<<Id>> AS ProductId,
                <<prd>>.<<Name>> AS ProductName
            FROM <<dbo>>.<<Products>> AS prd
            """ 
        },
        { 
            SqlBuilder.MySql(), 
            """
            SELECT 
                `prd`.`Id` AS ProductId,
                `prd`.`Name` AS ProductName
            FROM `dbo`.`Products` AS prd
            """ 
        },
        { 
            SqlBuilder.Oracle(), 
            """
            SELECT 
                "prd"."Id" AS ProductId,
                "prd"."Name" AS ProductName
            FROM "dbo"."Products" AS prd
            """ 
        },
        { 
            SqlBuilder.PostgreSql(), 
            """
            SELECT 
                "prd"."Id" AS ProductId,
                "prd"."Name" AS ProductName
            FROM "dbo"."Products" AS prd
            """ 
        },
        { 
            SqlBuilder.SqLite(), 
            """
            SELECT 
                "prd"."Id" AS ProductId,
                "prd"."Name" AS ProductName
            FROM "dbo"."Products" AS prd
            """ 
        },
        { 
            SqlBuilder.SqlServer(), 
            """
            SELECT 
                [prd].[Id] AS ProductId,
                [prd].[Name] AS ProductName
            FROM [dbo].[Products] AS prd
            """ 
        }
    };
}