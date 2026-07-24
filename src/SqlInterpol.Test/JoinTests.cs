using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class JoinTests
{
    [Theory]
    [MemberData(nameof(JoinTwoEntitiesData))]
    public void Join_TwoEntities(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act - Uses fluent entity initialization and zero-allocation properties
        testCase.Action(() => db.Entity<Product>(out var p)
            .Entity<OrderLine>(out var o)
            .Append($$"""
            SELECT
                {{p.Id}},
                {{o.OrderId}}
            FROM {{p}}
            JOIN {{o}}
                ON {{p.Id}} = {{o.ProductItemNumber}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> JoinTwoEntitiesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT
                    <<dbo>>.<<Products>>.<<Id>>,
                    <<OrderLine>>.<<OrderId>>
                FROM <<dbo>>.<<Products>>
                JOIN <<OrderLine>>
                    ON <<dbo>>.<<Products>>.<<Id>> = <<OrderLine>>.<<ProductItemNumber>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "dbo"."Products"."Id",
                    "OrderLine"."OrderId"
                FROM "dbo"."Products"
                JOIN "OrderLine"
                    ON "dbo"."Products"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
                    `dbo`.`Products`.`Id`,
                    `OrderLine`.`OrderId`
                FROM `dbo`.`Products`
                JOIN `OrderLine`
                    ON `dbo`.`Products`.`Id` = `OrderLine`.`ProductItemNumber`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT
                    "dbo"."Products"."Id",
                    "OrderLine"."OrderId"
                FROM "dbo"."Products"
                JOIN "OrderLine"
                    ON "dbo"."Products"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "dbo"."Products"."Id",
                    "OrderLine"."OrderId"
                FROM "dbo"."Products"
                JOIN "OrderLine"
                    ON "dbo"."Products"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "dbo"."Products"."Id",
                    "OrderLine"."OrderId"
                FROM "dbo"."Products"
                JOIN "OrderLine"
                    ON "dbo"."Products"."Id" = "OrderLine"."ProductItemNumber"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [dbo].[Products].[Id],
                    [OrderLine].[OrderId]
                FROM [dbo].[Products]
                JOIN [OrderLine]
                    ON [dbo].[Products].[Id] = [OrderLine].[ProductItemNumber]
                """
            ]
        )
    ];
}