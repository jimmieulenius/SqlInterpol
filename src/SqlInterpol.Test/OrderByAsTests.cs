using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class OrderByAsTests
{
    [Theory]
    [MemberData(nameof(OrderByWithExplicitAliasData))]
    public void OrderBy_WithExplicitAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => 
        {
            #pragma warning disable SQLIG10
            db.Entity<Product>(out var prod, "prod");
            return db.Append($$"""
                SELECT *
                FROM {{prod}} AS {{prod:alias}}
                ORDER BY
                    {{prod.Name}} ASC
                """).Build();
            #pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> OrderByWithExplicitAliasData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<dbo>>.<<Products>> AS <<prod>>
                ORDER BY
                    <<prod>>.<<PROD_NAME>> ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT *
                FROM "dbo"."Products" AS "prod"
                ORDER BY
                    "prod"."PROD_NAME" ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT *
                FROM `dbo`.`Products` AS `prod`
                ORDER BY
                    `prod`.`PROD_NAME` ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT *
                FROM "dbo"."Products" "prod"
                ORDER BY
                    "prod"."PROD_NAME" ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT *
                FROM "dbo"."Products" AS "prod"
                ORDER BY
                    "prod"."PROD_NAME" ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "dbo"."Products" AS "prod"
                ORDER BY
                    "prod"."PROD_NAME" ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [dbo].[Products] AS [prod]
                ORDER BY
                    [prod].[PROD_NAME] ASC
                """
            ]
        )
    ];
}