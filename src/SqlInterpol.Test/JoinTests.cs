using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class JoinTests
{
    [Theory]
    [MemberData(nameof(JoinTwoEntitiesData))]
    public void Join_TwoEntities(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<Product, OrderLine>((p, o) =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}},
                {{o[x => x.OrderId]}}
            FROM {{p}}
            JOIN {{o}}
                ON {{p[x => x.Id]}} = {{o[x => x.ProductItemNumber]}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
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