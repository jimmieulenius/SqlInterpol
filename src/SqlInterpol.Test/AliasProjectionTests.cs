using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Tests;

public class AliasProjectionTests
{
    [Theory]
    [MemberData(nameof(AppendLiteralAliasData))]
    public void Literal_Alias_Append(string _,SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>(p =>
             db.Append($"SELECT {p[x => x.Id]} AS ProductId FROM {p} AS prd"));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<string, SqlBuilder, string> AppendLiteralAliasData => new()
    {
        {
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<Id>> AS ProductId FROM <<dbo>>.<<Products>> AS prd"
        },
        {
            SqlDialectKind.MySql,
            SqlBuilder.MySql(),
            "SELECT `prd`.`Id` AS ProductId FROM `dbo`.`Products` AS prd"
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(),
            "SELECT [prd].[Id] AS ProductId FROM [dbo].[Products] AS prd"
        }
    };

    [Theory]
    [MemberData(nameof(AliasExplicitIsQuotedData))]
    public void Alias_Explicit_IsQuoted(string _,SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>(p =>
            db.Append($"SELECT {p[x => x.Id]} FROM {p} AS {p.Alias("prd")}"));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<string, SqlBuilder, string> AliasExplicitIsQuotedData => new()
    {
        {
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<Id>> FROM <<dbo>>.<<Products>> AS <<prd>>"
        },
        {
            SqlDialectKind.MySql,
            SqlBuilder.MySql(),
            "SELECT `prd`.`Id` FROM `dbo`.`Products` AS `prd`"
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(),
            "SELECT [prd].[Id] FROM [dbo].[Products] AS [prd]"
        }
    };

    [Theory]
    [MemberData(nameof(AliasLiteralInSqlIsUnquotedData))]
    public void Alias_LiteralInSql_IsUnquoted(string _,SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>(p =>
            db.Append($"SELECT {p[x => x.Id]} FROM {p} AS prd"));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<string, SqlBuilder, string> AliasLiteralInSqlIsUnquotedData => new()
    {
        {
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<Id>> FROM <<dbo>>.<<Products>> AS prd"
        },
        {
            SqlDialectKind.MySql,
            SqlBuilder.MySql(),
            "SELECT `prd`.`Id` FROM `dbo`.`Products` AS prd"
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(),
            "SELECT [prd].[Id] FROM [dbo].[Products] AS prd"
        }
    };

    [Theory]
    [MemberData(nameof(AppendQuotedAliasData))]
    public void QuotedAlias_Append(string _, SqlBuilder db, string expected)
    {
        // Arrange
        db.Query<Product>(p =>
             db.Append($"SELECT {p[x => x.Id]} AS {Sql.OpenQuote()}ProductId{Sql.CloseQuote()} FROM {p} AS {Sql.OpenQuote()}prd{Sql.CloseQuote()}"));

        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<string, SqlBuilder, string> AppendQuotedAliasData => new()
    {
        {
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<Id>> AS <<ProductId>> FROM <<dbo>>.<<Products>> AS <<prd>>"
        },
        {
            SqlDialectKind.MySql,
            SqlBuilder.MySql(),
            "SELECT `prd`.`Id` AS `ProductId` FROM `dbo`.`Products` AS `prd`"
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"Id\" AS \"ProductId\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"Id\" AS \"ProductId\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"Id\" AS \"ProductId\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(),
            "SELECT [prd].[Id] AS [ProductId] FROM [dbo].[Products] AS [prd]"
        }
    };

    [Theory]
    [MemberData(nameof(AppendLineAliasData))]
    public void Alias_AppendLine(string _, SqlBuilder db, string expected)
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

    public static TheoryData<string, SqlBuilder, string> AppendLineAliasData => new()
    {
        { 
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(), 
            $"SELECT <<prd>>.<<Id>> AS ProductId{Environment.NewLine
            }FROM <<dbo>>.<<Products>> AS prd" 
        },
        { 
            SqlDialectKind.MySql,
            SqlBuilder.MySql(), 
            $"SELECT `prd`.`Id` AS ProductId{Environment.NewLine
            }FROM `dbo`.`Products` AS prd" 
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(), 
            $"SELECT \"prd\".\"Id\" AS ProductId{Environment.NewLine
            }FROM \"dbo\".\"Products\" AS prd" 
        },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(), 
            $"SELECT \"prd\".\"Id\" AS ProductId{Environment.NewLine
            }FROM \"dbo\".\"Products\" AS prd" 
        },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(), 
            $"SELECT \"prd\".\"Id\" AS ProductId{Environment.NewLine
            }FROM \"dbo\".\"Products\" AS prd" 
        },
        { 
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(), 
            $"SELECT [prd].[Id] AS ProductId{Environment.NewLine
            }FROM [dbo].[Products] AS prd" 
        }
    };

    [Theory]
    [MemberData(nameof(RawStringAliasData))]
    public void Alias_RawString(string _, SqlBuilder db, string expected)
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

    public static TheoryData<string, SqlBuilder, string> RawStringAliasData => new()
    {
        { 
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(), 
            """
            SELECT 
                <<prd>>.<<Id>> AS ProductId,
                <<prd>>.<<PROD_NAME>> AS ProductName
            FROM <<dbo>>.<<Products>> AS prd
            """ 
        },
        {
            SqlDialectKind.MySql,
            SqlBuilder.MySql(), 
            """
            SELECT 
                `prd`.`Id` AS ProductId,
                `prd`.`PROD_NAME` AS ProductName
            FROM `dbo`.`Products` AS prd
            """ 
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(), 
            """
            SELECT 
                "prd"."Id" AS ProductId,
                "prd"."PROD_NAME" AS ProductName
            FROM "dbo"."Products" AS prd
            """ 
        },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(), 
            """
            SELECT 
                "prd"."Id" AS ProductId,
                "prd"."PROD_NAME" AS ProductName
            FROM "dbo"."Products" AS prd
            """ 
        },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(), 
            """
            SELECT 
                "prd"."Id" AS ProductId,
                "prd"."PROD_NAME" AS ProductName
            FROM "dbo"."Products" AS prd
            """ 
        },
        {
            SqlDialectKind.SqlServer,
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
    public void Alias_With_Reserved_Words_Preserves_Quotes_And_Mapping(string _, SqlBuilder db, string expected)
    {
        db.Query<Product>(p =>
            db.Append($@"SELECT {p[x => x.Id]} AS {Sql.OpenQuote()}Order{Sql.CloseQuote()} FROM {p} AS {Sql.OpenQuote()}Group{Sql.CloseQuote()}"));
        
        // Act
        var result = db.Build();
        
        // Assert
        Assert.Equal(expected, result.Sql);
    }

    public static TheoryData<string, SqlBuilder, string> ReservedAliasData => new()
    {
        { 
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(),   
            "SELECT <<Group>>.<<Id>> AS <<Order>> FROM <<dbo>>.<<Products>> AS <<Group>>" 
        },
        { 
            SqlDialectKind.MySql,
            SqlBuilder.MySql(),      
            "SELECT `Group`.`Id` AS `Order` FROM `dbo`.`Products` AS `Group`" 
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(),    
            "SELECT \"Group\".\"Id\" AS \"Order\" FROM \"dbo\".\"Products\" AS \"Group\"" 
        },
        { 
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(),    
            "SELECT \"Group\".\"Id\" AS \"Order\" FROM \"dbo\".\"Products\" AS \"Group\"" 
        },
        { 
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(),    
            "SELECT \"Group\".\"Id\" AS \"Order\" FROM \"dbo\".\"Products\" AS \"Group\"" 
        },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(), 
            "SELECT [Group].[Id] AS [Order] FROM [dbo].[Products] AS [Group]" 
        }
    };

    [Theory]
    [MemberData(nameof(AliasMappingIdentityData))]
    public void Alias_Mapping_Identity(string _, SqlBuilder db, string expected)
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

    public static TheoryData<string, SqlBuilder, string> AliasMappingIdentityData => new()
    {
        {
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<PROD_NAME>> AS <<Name>> FROM <<dbo>>.<<Products>> AS <<prd>>"
        },
        {
            SqlDialectKind.MySql,
            SqlBuilder.MySql(),
            "SELECT `prd`.`PROD_NAME` AS `Name` FROM `dbo`.`Products` AS `prd`"
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(),
            "SELECT [prd].[PROD_NAME] AS [Name] FROM [dbo].[Products] AS [prd]"
        }
    };

    [Theory]
    [MemberData(nameof(AliasMappingUnquotedData))]
    public void Alias_Mapping_Unquoted(string _, SqlBuilder db, string expected)
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

    public static TheoryData<string, SqlBuilder, string> AliasMappingUnquotedData => new()
    {
        {
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<PROD_NAME>> AS <<Name>> FROM <<dbo>>.<<Products>> AS prd"
        },
        {
            SqlDialectKind.MySql,
            SqlBuilder.MySql(),
            "SELECT `prd`.`PROD_NAME` AS `Name` FROM `dbo`.`Products` AS prd"
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS prd"
        },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(),
            "SELECT [prd].[PROD_NAME] AS [Name] FROM [dbo].[Products] AS prd"
        }
    };

    [Theory]
    [MemberData(nameof(ManualColumnData))]
    public void Manual_Column_With_Alias_Theory(string _,SqlBuilder db, string expected)
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

    public static TheoryData<string, SqlBuilder, string> ManualColumnData => new()
    {
        {
            SqlDialectKind.CustomDb,
            SqlBuilder.CustomDb(),
            "SELECT <<prd>>.<<LEGACY_PROD_NAME>> AS <<Name>> FROM <<dbo>>.<<Products>> AS <<prd>>"
        },
        {
            SqlDialectKind.MySql,
            SqlBuilder.MySql(),
            "SELECT `prd`.`LEGACY_PROD_NAME` AS `Name` FROM `dbo`.`Products` AS `prd`"
        },
        {
            SqlDialectKind.Oracle,
            SqlBuilder.Oracle(),
            "SELECT \"prd\".\"LEGACY_PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.PostgreSql,
            SqlBuilder.PostgreSql(),
            "SELECT \"prd\".\"LEGACY_PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.SqLite,
            SqlBuilder.SqLite(),
            "SELECT \"prd\".\"LEGACY_PROD_NAME\" AS \"Name\" FROM \"dbo\".\"Products\" AS \"prd\""
        },
        {
            SqlDialectKind.SqlServer,
            SqlBuilder.SqlServer(),
            "SELECT [prd].[LEGACY_PROD_NAME] AS [Name] FROM [dbo].[Products] AS [prd]"
        }
    };
}