using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class GroupByAsTests
{
    [Theory]
    [MemberData(nameof(GroupByWithExplicitAliasData))]
    public void GroupBy_WithExplicitAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        // Using Name here proves that [SqlColumn("PROD_NAME")] still resolves perfectly 
        // through the alias prefix!
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT {{p[x => x.Name]}}, {{p[x => x.IsActive]}}, COUNT(*)
            FROM {{p}} AS {{p.As("prod")}}
            GROUP BY {{p[x => x.Name]}}, {{p[x => x.IsActive]}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> GroupByWithExplicitAliasData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<prod>>.<<PROD_NAME>>, <<prod>>.<<IsActive>>, COUNT(*)
                FROM <<dbo>>.<<Products>> AS <<prod>>
                GROUP BY <<prod>>.<<PROD_NAME>>, <<prod>>.<<IsActive>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT `prod`.`PROD_NAME`, `prod`.`IsActive`, COUNT(*)
                FROM `dbo`.`Products` AS `prod`
                GROUP BY `prod`.`PROD_NAME`, `prod`.`IsActive`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT "prod"."PROD_NAME", "prod"."IsActive", COUNT(*)
                FROM "dbo"."Products" AS "prod"
                GROUP BY "prod"."PROD_NAME", "prod"."IsActive"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT "prod"."PROD_NAME", "prod"."IsActive", COUNT(*)
                FROM "dbo"."Products" AS "prod"
                GROUP BY "prod"."PROD_NAME", "prod"."IsActive"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "prod"."PROD_NAME", "prod"."IsActive", COUNT(*)
                FROM "dbo"."Products" AS "prod"
                GROUP BY "prod"."PROD_NAME", "prod"."IsActive"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            [
                """
                SELECT [prod].[PROD_NAME], [prod].[IsActive], COUNT(*)
                FROM [dbo].[Products] AS [prod]
                GROUP BY [prod].[PROD_NAME], [prod].[IsActive]
                """
            ]
        )
    ];
}