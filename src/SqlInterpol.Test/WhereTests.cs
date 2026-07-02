using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;
using System.Linq;

namespace SqlInterpol.Test;

public class WhereTests
{
    [Theory]
    [MemberData(nameof(WhereSimpleParameterData))]
    public void Where_SimpleParameter(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int targetId = 42;

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT
                {{p.Id}}
            FROM {{p}}
            WHERE {{p.Id}} = {{targetId}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(WhereInCollectionData))]
    public void Where_InCollection(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int[] categoryIds = [10, 20, 30];

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT
                {{p.Id}}
            FROM {{p}}
            WHERE {{p.CategoryId}} IN ({{categoryIds}})
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> WhereSimpleParameterData
    {
        get
        {
            object?[] expectedParams = [42];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        SELECT
                            <<dbo>>.<<Products>>.<<Id>>
                        FROM <<dbo>>.<<Products>>
                        WHERE <<dbo>>.<<Products>>.<<Id>> = !!100
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        SELECT
                            "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Id" = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT
                            `dbo`.`Products`.`Id`
                        FROM `dbo`.`Products`
                        WHERE `dbo`.`Products`.`Id` = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle, 
                    [
                        """
                        SELECT
                            "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Id" = :0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql, 
                    [
                        """
                        SELECT
                            "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Id" = $1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        SELECT
                            "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Id" = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT
                            [dbo].[Products].[Id]
                        FROM [dbo].[Products]
                        WHERE [dbo].[Products].[Id] = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> WhereInCollectionData
    {
        get
        {
            object?[] expectedParams = [10, 20, 30];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb, 
                    [
                        """
                        SELECT
                            <<dbo>>.<<Products>>.<<Id>>
                        FROM <<dbo>>.<<Products>>
                        WHERE <<dbo>>.<<Products>>.<<CategoryId>> IN (!!100, !!101, !!102)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        SELECT
                            "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."CategoryId" IN (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql, 
                    [
                        """
                        SELECT
                            `dbo`.`Products`.`Id`
                        FROM `dbo`.`Products`
                        WHERE `dbo`.`Products`.`CategoryId` IN (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        SELECT
                            "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."CategoryId" IN (:0, :1, :2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql, 
                    [
                        """
                        SELECT
                            "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."CategoryId" IN ($1, $2, $3)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        SELECT
                            "dbo"."Products"."Id"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."CategoryId" IN (@p1, @p2, @p3)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT
                            [dbo].[Products].[Id]
                        FROM [dbo].[Products]
                        WHERE [dbo].[Products].[CategoryId] IN (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}