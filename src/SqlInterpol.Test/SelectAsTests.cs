using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Tests;

public class SelectAsTests
{
    [Theory]
    [MemberData(nameof(AppendLiteralAliasData))]
    public void Literal_Alias_Append(string _,SqlBuilder db, string expected)
    {
        db.Query<Product>(p =>
             db.Append($"SELECT {p[x => x.Id]} AS ProductId FROM {p} AS prd"));
        var result = db.Build();
        Assert.Equal(expected, result.Sql);
    }
    public static TheoryData<string, SqlBuilder, string> AppendLiteralAliasData => new()
    {
        { SqlDialectKind.CustomDb, SqlBuilder.CustomDb(), "SELECT <<prd>>.<<Id>> AS ProductId FROM <<dbo>>.<<Products>> AS prd" },
        { SqlDialectKind.MySql, SqlBuilder.MySql(), "SELECT `prd`.`Id` AS ProductId FROM `dbo`.`Products` AS prd" },
        { SqlDialectKind.Oracle, SqlBuilder.Oracle(), "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd" },
        { SqlDialectKind.PostgreSql, SqlBuilder.PostgreSql(), "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd" },
        { SqlDialectKind.SqLite, SqlBuilder.SqLite(), "SELECT \"prd\".\"Id\" AS ProductId FROM \"dbo\".\"Products\" AS prd" },
        { SqlDialectKind.SqlServer, SqlBuilder.SqlServer(), "SELECT [prd].[Id] AS ProductId FROM [dbo].[Products] AS prd" }
    };

    [Theory]
    [MemberData(nameof(AliasExplicitIsQuotedData))]
    public void Alias_Explicit_IsQuoted(string _,SqlBuilder db, string expected)
    {
        db.Query<Product>(p =>
            db.Append($"SELECT {p[x => x.Id]} FROM {p} AS {p.Alias("prd")}"));
        var result = db.Build();
        Assert.Equal(expected, result.Sql);
    }
    public static TheoryData<string, SqlBuilder, string> AliasExplicitIsQuotedData => new()
    {
        { SqlDialectKind.CustomDb, SqlBuilder.CustomDb(), "SELECT <<prd>>.<<Id>> FROM <<dbo>>.<<Products>> AS <<prd>>" },
        { SqlDialectKind.MySql, SqlBuilder.MySql(), "SELECT `prd`.`Id` FROM `dbo`.`Products` AS `prd`" },
        { SqlDialectKind.Oracle, SqlBuilder.Oracle(), "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS \"prd\"" },
        { SqlDialectKind.PostgreSql, SqlBuilder.PostgreSql(), "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS \"prd\"" },
        { SqlDialectKind.SqLite, SqlBuilder.SqLite(), "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS \"prd\"" },
        { SqlDialectKind.SqlServer, SqlBuilder.SqlServer(), "SELECT [prd].[Id] FROM [dbo].[Products] AS [prd]" }
    };

    [Theory]
    [MemberData(nameof(AliasLiteralInSqlIsUnquotedData))]
    public void Alias_LiteralInSql_IsUnquoted(string _,SqlBuilder db, string expected)
    {
        db.Query<Product>(p =>
            db.Append($"SELECT {p[x => x.Id]} FROM {p} AS prd"));
        var result = db.Build();
        Assert.Equal(expected, result.Sql);
    }
    public static TheoryData<string, SqlBuilder, string> AliasLiteralInSqlIsUnquotedData => new()
    {
        { SqlDialectKind.CustomDb, SqlBuilder.CustomDb(), "SELECT <<prd>>.<<Id>> FROM <<dbo>>.<<Products>> AS prd" },
        { SqlDialectKind.MySql, SqlBuilder.MySql(), "SELECT `prd`.`Id` FROM `dbo`.`Products` AS prd" },
        { SqlDialectKind.Oracle, SqlBuilder.Oracle(), "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS prd" },
        { SqlDialectKind.PostgreSql, SqlBuilder.PostgreSql(), "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS prd" },
        { SqlDialectKind.SqLite, SqlBuilder.SqLite(), "SELECT \"prd\".\"Id\" FROM \"dbo\".\"Products\" AS prd" },
        { SqlDialectKind.SqlServer, SqlBuilder.SqlServer(), "SELECT [prd].[Id] FROM [dbo].[Products] AS prd" }
    };
}
