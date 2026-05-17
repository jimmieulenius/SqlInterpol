using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

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
        var result = db.Query<Product>(p =>
            db.Append($$"""
                SELECT 
                    {{p[x => x.CategoryId]}},
                    COUNT({{p[x => x.Id]}}) AS ProductCount
                FROM {{p}}
                GROUP BY {{p[x => x.CategoryId]}}
                HAVING COUNT({{p[x => x.Id]}}) > {{5}}
                """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
        
        // Verify the parameter (> 5) was successfully hoisted from the HAVING clause
        Assert.Single(result.Parameters);
        Assert.Equal(5, result.Parameters.Values.First());
    }

    public static TheoryData<SqlTestCase> SelectGroupByAndHavingData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT 
                    <<dbo>>.<<Products>>.<<CategoryId>>,
                    COUNT(<<dbo>>.<<Products>>.<<Id>>) AS ProductCount
                FROM <<dbo>>.<<Products>>
                GROUP BY <<dbo>>.<<Products>>.<<CategoryId>>
                HAVING COUNT(<<dbo>>.<<Products>>.<<Id>>) > !!100
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT 
                    `dbo`.`Products`.`CategoryId`,
                    COUNT(`dbo`.`Products`.`Id`) AS ProductCount
                FROM `dbo`.`Products`
                GROUP BY `dbo`.`Products`.`CategoryId`
                HAVING COUNT(`dbo`.`Products`.`Id`) > @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT 
                    "dbo"."Products"."CategoryId",
                    COUNT("dbo"."Products"."Id") AS ProductCount
                FROM "dbo"."Products"
                GROUP BY "dbo"."Products"."CategoryId"
                HAVING COUNT("dbo"."Products"."Id") > :0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT 
                    "dbo"."Products"."CategoryId",
                    COUNT("dbo"."Products"."Id") AS ProductCount
                FROM "dbo"."Products"
                GROUP BY "dbo"."Products"."CategoryId"
                HAVING COUNT("dbo"."Products"."Id") > $1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT 
                    "dbo"."Products"."CategoryId",
                    COUNT("dbo"."Products"."Id") AS ProductCount
                FROM "dbo"."Products"
                GROUP BY "dbo"."Products"."CategoryId"
                HAVING COUNT("dbo"."Products"."Id") > ?0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT 
                    [dbo].[Products].[CategoryId],
                    COUNT([dbo].[Products].[Id]) AS ProductCount
                FROM [dbo].[Products]
                GROUP BY [dbo].[Products].[CategoryId]
                HAVING COUNT([dbo].[Products].[Id]) > @p0
                """
            ]
        )
    ];
}