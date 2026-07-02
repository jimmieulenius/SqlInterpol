using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class WhereAsTests
{
    [Theory]
    [MemberData(nameof(WhereWithAliasedEntityData))]
    public void Where_WithAliasedEntityAndColumn(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var productId = 1;
        var categoryId = 5;

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT
                {{p.Id}} AS ProductId
            FROM {{p}} AS p
            WHERE {{p.Id}} = {{productId}} AND {{p.CategoryId}} = {{categoryId}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> WhereWithAliasedEntityData
    {
        get
        {
            object?[] expectedParams = [1, 5];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb, 
                    [
                        """
                        SELECT
                            <<p>>.<<Id>> AS <<ProductId>>
                        FROM <<dbo>>.<<Products>> AS <<p>>
                        WHERE <<p>>.<<Id>> = !!100 AND <<p>>.<<CategoryId>> = !!101
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        SELECT
                            "p"."Id" AS "ProductId"
                        FROM "dbo"."Products" AS "p"
                        WHERE "p"."Id" = @p0 AND "p"."CategoryId" = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT
                            `p`.`Id` AS `ProductId`
                        FROM `dbo`.`Products` AS `p`
                        WHERE `p`.`Id` = @p0 AND `p`.`CategoryId` = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle, 
                    [
                        """
                        SELECT
                            "p"."Id" AS "ProductId"
                        FROM "dbo"."Products" "p"
                        WHERE "p"."Id" = :0 AND "p"."CategoryId" = :1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql, 
                    [
                        """
                        SELECT
                            "p"."Id" AS "ProductId"
                        FROM "dbo"."Products" AS "p"
                        WHERE "p"."Id" = $1 AND "p"."CategoryId" = $2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite, 
                    [
                        """
                        SELECT
                            "p"."Id" AS "ProductId"
                        FROM "dbo"."Products" AS "p"
                        WHERE "p"."Id" = @p1 AND "p"."CategoryId" = @p2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT
                            [p].[Id] AS [ProductId]
                        FROM [dbo].[Products] AS [p]
                        WHERE [p].[Id] = @p0 AND [p].[CategoryId] = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}