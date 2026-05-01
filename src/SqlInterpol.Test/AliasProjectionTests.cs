using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Tests;

public class AliasProjectionTests
{
    [Theory]
    [MemberData(nameof(AppendAliasData))]
    public void Alias_Append(SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>(p =>
             db.Append($"SELECT {p[x => x.Id]} AS ProductId FROM {p} AS prd"));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> AppendAliasData => new()
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
    [MemberData(nameof(AppendQuotedAliasData))]
    public void QuotedAlias_Append(SqlBuilder db, string expected)
    {
        // Arrange
        var open = Sql.Raw(db.Context.Dialect.OpenQuote);
        var close = Sql.Raw(db.Context.Dialect.CloseQuote);

        db.Query<Product>(p =>
             db.Append($"SELECT {p[x => x.Id]} AS {open}ProductId{close} FROM {p} AS {open}prd{close}"));

        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> AppendQuotedAliasData => new()
    {
        {
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<Id>> AS <<ProductId>> FROM <<dbo>>.<<Products>> AS <<prd>>"
        },
        {
            SqlBuilder.MySql(),
            "SELECT `prd`.`Id` AS `ProductId` FROM `dbo`.`Products` AS `prd`"
        },
        {
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"Id\" AS \"ProductId\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"Id\" AS \"ProductId\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"Id\" AS \"ProductId\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlBuilder.SqlServer(),
            "SELECT [prd].[Id] AS [ProductId] FROM [dbo].[Products] AS [prd]"
        }
    };

    [Theory]
    [MemberData(nameof(AppendLineAliasData))]
    public void Alias_AppendLine(SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>(p =>
             db.AppendLine($"SELECT {p[x => x.Id]} AS ProductId")
             .Append($"FROM {p} AS prd"));

        // Act
        var result = db.Build();

        // Assert
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
        // Arrange
        db.Query<Product>(p =>
             db.Append($$"""
            SELECT 
                {{p[x => x.Id]}} AS ProductId,
                {{p[x => x.Name]}} AS ProductName
            FROM {{p}} AS prd
            """));

        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> RawStringAliasData => new()
    {
        { 
            SqlBuilder.CustomDb(), 
            """
            SELECT 
                <<prd>>.<<Id>> AS ProductId,
                <<prd>>.<<PROD_NAME>> AS ProductName
            FROM <<dbo>>.<<Products>> AS prd
            """ 
        },
        { 
            SqlBuilder.MySql(), 
            """
            SELECT 
                `prd`.`Id` AS ProductId,
                `prd`.`PROD_NAME` AS ProductName
            FROM `dbo`.`Products` AS prd
            """ 
        },
        { 
            SqlBuilder.Oracle(), 
            """
            SELECT 
                "prd"."Id" AS ProductId,
                "prd"."PROD_NAME" AS ProductName
            FROM "dbo"."Products" AS prd
            """ 
        },
        { 
            SqlBuilder.PostgreSql(), 
            """
            SELECT 
                "prd"."Id" AS ProductId,
                "prd"."PROD_NAME" AS ProductName
            FROM "dbo"."Products" AS prd
            """ 
        },
        { 
            SqlBuilder.SqLite(), 
            """
            SELECT 
                "prd"."Id" AS ProductId,
                "prd"."PROD_NAME" AS ProductName
            FROM "dbo"."Products" AS prd
            """ 
        },
        { 
            SqlBuilder.SqlServer(), 
            """
            SELECT 
                [prd].[Id] AS ProductId,
                [prd].[PROD_NAME] AS ProductName
            FROM [dbo].[Products] AS prd
            """ 
        }
    };

    [Theory]
    [MemberData(nameof(ReservedAliasData))]
    public void Alias_With_Reserved_Words_Preserves_Quotes_And_Mapping(SqlBuilder db, string expected)
    {
        // Arrange
        var open = Sql.Raw(db.Context.Dialect.OpenQuote);
        var close = Sql.Raw(db.Context.Dialect.CloseQuote);

        db.Query<Product>(p =>
            db.Append($@"SELECT {p[x => x.Id]} AS {open}Order{close} FROM {p} AS {open}Group{close}"));
        
        // Act
        var result = db.Build();
        
        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> ReservedAliasData => new()
    {
        { 
            SqlBuilder.CustomDb(),   
            "SELECT <<Group>>.<<Id>> AS <<Order>> FROM <<dbo>>.<<Products>> AS <<Group>>" 
        },
        { 
            SqlBuilder.MySql(),      
            "SELECT `Group`.`Id` AS `Order` FROM `dbo`.`Products` AS `Group`" 
        },
        { 
            SqlBuilder.Oracle(),    
            "SELECT \"Group\".\"Id\" AS \"Order\" FROM \"dbo\".\"Products\" AS \"Group\"" 
        },
        { 
            SqlBuilder.PostgreSql(),    
            "SELECT \"Group\".\"Id\" AS \"Order\" FROM \"dbo\".\"Products\" AS \"Group\"" 
        },
        { 
            SqlBuilder.SqLite(),    
            "SELECT \"Group\".\"Id\" AS \"Order\" FROM \"dbo\".\"Products\" AS \"Group\"" 
        },
        { 
            SqlBuilder.SqlServer(), 
            "SELECT [Group].[Id] AS [Order] FROM [dbo].[Products] AS [Group]" 
        }
    };

    [Theory]
    [MemberData(nameof(AliasMappingIdentityData))]
    public void Alias_Mapping_Identity(SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>(p =>
            db.Append($"SELECT {p[x => x.Name]} AS {p[x => x.Name]} ")
            .Append($"FROM {p} AS {p.Alias("prd")}"));

        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> AliasMappingIdentityData => new()
    {
        {
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<PROD_NAME>> AS <<Name>> FROM <<dbo>>.<<Products>> AS <<prd>>"
        },
        {
            SqlBuilder.MySql(),
            "SELECT `prd`.`PROD_NAME` AS `Name` FROM `dbo`.`Products` AS `prd`"
        },
        {
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlBuilder.SqlServer(),
            "SELECT [prd].[PROD_NAME] AS [Name] FROM [dbo].[Products] AS [prd]"
        }
    };

    [Theory]
    [MemberData(nameof(AliasMappingUnquotedData))]
    public void Alias_Mapping_Unquoted(SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>(p =>
            db.Append($"SELECT {p[x => x.Name]} AS {p[x => x.Name]} ")
            .Append($"FROM {p} AS prd"));

        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> AliasMappingUnquotedData => new()
    {
        {
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<PROD_NAME>> AS <<Name>> FROM <<dbo>>.<<Products>> AS prd"
        },
        {
            SqlBuilder.MySql(),
            "SELECT `prd`.`PROD_NAME` AS `Name` FROM `dbo`.`Products` AS prd"
        },
        {
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlBuilder.SqlServer(),
            "SELECT [prd].[PROD_NAME] AS [Name] FROM [dbo].[Products] AS prd"
        }
    };

    [Theory]
    [MemberData(nameof(ManualColumnData))]
    public void Manual_Column_With_Alias_Theory(SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>(p =>
            db.Append($"SELECT {p.Column("LEGACY_PROD_NAME")} AS {p[x => x.Name]} ")
            .Append($"FROM {p} AS {p.Alias("prd")}"));

        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<SqlBuilder, string> ManualColumnData => new()
    {
        {
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<LEGACY_PROD_NAME>> AS <<Name>> FROM <<dbo>>.<<Products>> AS <<prd>>" },
        {
            SqlBuilder.MySql(),
            "SELECT `prd`.`LEGACY_PROD_NAME` AS `Name` FROM `dbo`.`Products` AS `prd`" },
        {
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"LEGACY_PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\"" },
        {
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"LEGACY_PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\"" },
        {
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"LEGACY_PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\"" },
        {
            SqlBuilder.SqlServer(),
            "SELECT [prd].[LEGACY_PROD_NAME] AS [Name] FROM [dbo].[Products] AS [prd]"
        }
    };
}