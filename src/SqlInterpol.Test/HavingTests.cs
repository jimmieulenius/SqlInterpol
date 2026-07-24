using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class HavingTests
{
    [Theory]
    [MemberData(nameof(SelectGroupByAndHavingData))]
    public void Select_GroupByAndHaving(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
                SELECT 
                    {{p.CategoryId}},
                    COUNT({{p.Id}}) AS ProductCount
                FROM {{p}}
                GROUP BY {{p.CategoryId}}
                HAVING COUNT({{p.Id}}) > {{5}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> SelectGroupByAndHavingData
    {
        get
        {
            object?[] expectedParams = [5];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        SELECT 
                            <<dbo>>.<<Products>>.<<CategoryId>>,
                            COUNT(<<dbo>>.<<Products>>.<<Id>>) AS <<ProductCount>>
                        FROM <<dbo>>.<<Products>>
                        GROUP BY <<dbo>>.<<Products>>.<<CategoryId>>
                        HAVING COUNT(<<dbo>>.<<Products>>.<<Id>>) > !!100
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        SELECT 
                            "dbo"."Products"."CategoryId",
                            COUNT("dbo"."Products"."Id") AS "ProductCount"
                        FROM "dbo"."Products"
                        GROUP BY "dbo"."Products"."CategoryId"
                        HAVING COUNT("dbo"."Products"."Id") > @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT 
                            `dbo`.`Products`.`CategoryId`,
                            COUNT(`dbo`.`Products`.`Id`) AS `ProductCount`
                        FROM `dbo`.`Products`
                        GROUP BY `dbo`.`Products`.`CategoryId`
                        HAVING COUNT(`dbo`.`Products`.`Id`) > @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        SELECT 
                            "dbo"."Products"."CategoryId",
                            COUNT("dbo"."Products"."Id") AS "ProductCount"
                        FROM "dbo"."Products"
                        GROUP BY "dbo"."Products"."CategoryId"
                        HAVING COUNT("dbo"."Products"."Id") > :0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        SELECT 
                            "dbo"."Products"."CategoryId",
                            COUNT("dbo"."Products"."Id") AS "ProductCount"
                        FROM "dbo"."Products"
                        GROUP BY "dbo"."Products"."CategoryId"
                        HAVING COUNT("dbo"."Products"."Id") > $1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        SELECT 
                            "dbo"."Products"."CategoryId",
                            COUNT("dbo"."Products"."Id") AS "ProductCount"
                        FROM "dbo"."Products"
                        GROUP BY "dbo"."Products"."CategoryId"
                        HAVING COUNT("dbo"."Products"."Id") > @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT 
                            [dbo].[Products].[CategoryId],
                            COUNT([dbo].[Products].[Id]) AS [ProductCount]
                        FROM [dbo].[Products]
                        GROUP BY [dbo].[Products].[CategoryId]
                        HAVING COUNT([dbo].[Products].[Id]) > @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}