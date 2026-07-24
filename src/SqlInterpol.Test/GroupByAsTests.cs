using SqlInterpol.Configuration;
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
        testCase.Action(() => db
            .Entity<Product>(out var p)
            .Append($$"""
                SELECT {{p.Name}}, {{p.IsActive}}, COUNT(*)
                FROM {{p}} AS prod
                GROUP BY {{p.Name}}, {{p.IsActive}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
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
            SqlDialectKind.Firebird,
            [
                """
                SELECT "prod"."PROD_NAME", "prod"."IsActive", COUNT(*)
                FROM "dbo"."Products" AS "prod"
                GROUP BY "prod"."PROD_NAME", "prod"."IsActive"
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
                FROM "dbo"."Products" "prod"
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