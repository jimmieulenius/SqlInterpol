using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class RenderExtensionTests
{
    [Theory]
    [MemberData(nameof(AsDeclarationData))]
    public void RenderExtension_AsDeclaration(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        testCase.Action(() =>
        {
            db.Entity<Product>(out var p, "prod");
            return db.Append($"SELECT * FROM {p.AsDeclaration()}").Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [Theory]
    [MemberData(nameof(AsAliasData))]
    public void RenderExtension_AsAlias(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        testCase.Action(() =>
        {
            db.Entity<Product>(out var p, "prod");
            return db.Append($"SELECT {p.AsAlias()}.* FROM dbo.Products AS {p.AsAlias()}").Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [Theory]
    [MemberData(nameof(AsBaseData))]
    public void RenderExtension_AsBase(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        testCase.Action(() =>
        {
            db.Entity<Product>(out var p, "prod");
            return db.Append($"TRUNCATE TABLE {p.AsBase()}").Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [Theory]
    [MemberData(nameof(AsColumnData))]
    public void RenderExtension_AsColumn(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        testCase.Action(() =>
        {
            db.Entity<Product>(out var p, "prod");
            return db.Append($"SELECT {p.Name.AsColumn()} FROM {p}").Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [Theory]
    [MemberData(nameof(CombinedData))]
    public void RenderExtension_Combined(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        testCase.Action(() =>
        {
            db.Entity<Product>(out var p, "prod");
            
            // Declare without an explicit alias; the tool's inline "AS" parser will automatically link it
            db.Entity<Product>(out var backup); 
            
            return db.Append($$"""
                SELECT 
                    {{p.Id.AsColumn()}}, 
                    {{p.AsAlias()}}.{{p.Name.AsColumn()}}
                FROM {{p.AsDeclaration()}}
                INNER JOIN {{backup}} AS backup_prod ON {{backup.AsAlias()}}.Id = {{p.AsAlias()}}.Id
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted(); 
    }

    // --- TEST DATA ---

    public static TheoryData<SqlTestCase> AsDeclarationData =>
    [
        new SqlTestCase(SqlDialectKind.CustomDb, ["SELECT * FROM <<dbo>>.<<Products>> AS <<prod>>"]),
        new SqlTestCase(SqlDialectKind.Firebird, ["SELECT * FROM \"dbo\".\"Products\" AS \"prod\""]),
        new SqlTestCase(SqlDialectKind.MySql, ["SELECT * FROM `dbo`.`Products` AS `prod`"]),
        new SqlTestCase(SqlDialectKind.Oracle, ["SELECT * FROM \"dbo\".\"Products\" \"prod\""]),
        new SqlTestCase(SqlDialectKind.PostgreSql, ["SELECT * FROM \"dbo\".\"Products\" AS \"prod\""]),
        new SqlTestCase(SqlDialectKind.SqLite, ["SELECT * FROM \"dbo\".\"Products\" AS \"prod\""]),
        new SqlTestCase(SqlDialectKind.SqlServer, ["SELECT * FROM [dbo].[Products] AS [prod]"])
    ];

    public static TheoryData<SqlTestCase> AsAliasData =>
    [
        new SqlTestCase(SqlDialectKind.CustomDb, ["SELECT <<prod>>.* FROM dbo.Products AS <<prod>>"]),
        new SqlTestCase(SqlDialectKind.Firebird, ["SELECT \"prod\".* FROM dbo.Products AS \"prod\""]),
        new SqlTestCase(SqlDialectKind.MySql, ["SELECT `prod`.* FROM dbo.Products AS `prod`"]),
        new SqlTestCase(SqlDialectKind.Oracle, ["SELECT \"prod\".* FROM dbo.Products \"prod\""]),
        new SqlTestCase(SqlDialectKind.PostgreSql, ["SELECT \"prod\".* FROM dbo.Products AS \"prod\""]),
        new SqlTestCase(SqlDialectKind.SqLite, ["SELECT \"prod\".* FROM dbo.Products AS \"prod\""]),
        new SqlTestCase(SqlDialectKind.SqlServer, ["SELECT [prod].* FROM dbo.Products AS [prod]"])
    ];

    public static TheoryData<SqlTestCase> AsBaseData =>
    [
        new SqlTestCase(SqlDialectKind.CustomDb, ["TRUNCATE TABLE <<dbo>>.<<Products>>"]),
        new SqlTestCase(SqlDialectKind.Firebird, ["TRUNCATE TABLE \"dbo\".\"Products\""]),
        new SqlTestCase(SqlDialectKind.MySql, ["TRUNCATE TABLE `dbo`.`Products`"]),
        new SqlTestCase(SqlDialectKind.Oracle, ["TRUNCATE TABLE \"dbo\".\"Products\""]),
        new SqlTestCase(SqlDialectKind.PostgreSql, ["TRUNCATE TABLE \"dbo\".\"Products\""]),
        new SqlTestCase(SqlDialectKind.SqLite, ["TRUNCATE TABLE \"dbo\".\"Products\""]),
        new SqlTestCase(SqlDialectKind.SqlServer, ["TRUNCATE TABLE [dbo].[Products]"])
    ];

    public static TheoryData<SqlTestCase> AsColumnData =>
    [
        new SqlTestCase(SqlDialectKind.CustomDb, ["SELECT <<PROD_NAME>> FROM <<dbo>>.<<Products>> AS <<prod>>"]),
        new SqlTestCase(SqlDialectKind.Firebird, ["SELECT \"PROD_NAME\" FROM \"dbo\".\"Products\" AS \"prod\""]),
        new SqlTestCase(SqlDialectKind.MySql, ["SELECT `PROD_NAME` FROM `dbo`.`Products` AS `prod`"]),
        new SqlTestCase(SqlDialectKind.Oracle, ["SELECT \"PROD_NAME\" FROM \"dbo\".\"Products\" \"prod\""]),
        new SqlTestCase(SqlDialectKind.PostgreSql, ["SELECT \"PROD_NAME\" FROM \"dbo\".\"Products\" AS \"prod\""]),
        new SqlTestCase(SqlDialectKind.SqLite, ["SELECT \"PROD_NAME\" FROM \"dbo\".\"Products\" AS \"prod\""]),
        new SqlTestCase(SqlDialectKind.SqlServer, ["SELECT [PROD_NAME] FROM [dbo].[Products] AS [prod]"])
    ];

    public static TheoryData<SqlTestCase> CombinedData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT 
                    <<Id>>, 
                    <<prod>>.<<PROD_NAME>>
                FROM <<dbo>>.<<Products>> AS <<prod>>
                INNER JOIN <<dbo>>.<<Products>> AS <<backup_prod>> ON <<backup_prod>>.Id = <<prod>>.Id
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT 
                    "Id", 
                    "prod"."PROD_NAME"
                FROM "dbo"."Products" AS "prod"
                INNER JOIN "dbo"."Products" AS "backup_prod" ON "backup_prod".Id = "prod".Id
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT 
                    `Id`, 
                    `prod`.`PROD_NAME`
                FROM `dbo`.`Products` AS `prod`
                INNER JOIN `dbo`.`Products` AS `backup_prod` ON `backup_prod`.Id = `prod`.Id
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT 
                    "Id", 
                    "prod"."PROD_NAME"
                FROM "dbo"."Products" "prod"
                INNER JOIN "dbo"."Products" "backup_prod" ON "backup_prod".Id = "prod".Id
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT 
                    "Id", 
                    "prod"."PROD_NAME"
                FROM "dbo"."Products" AS "prod"
                INNER JOIN "dbo"."Products" AS "backup_prod" ON "backup_prod".Id = "prod".Id
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT 
                    "Id", 
                    "prod"."PROD_NAME"
                FROM "dbo"."Products" AS "prod"
                INNER JOIN "dbo"."Products" AS "backup_prod" ON "backup_prod".Id = "prod".Id
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT 
                    [Id], 
                    [prod].[PROD_NAME]
                FROM [dbo].[Products] AS [prod]
                INNER JOIN [dbo].[Products] AS [backup_prod] ON [backup_prod].Id = [prod].Id
                """
            ]
        )
    ];
}