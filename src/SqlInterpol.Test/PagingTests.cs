using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class PagingTests
{
    [Theory]
    [MemberData(nameof(AllDialectsPagingData))]
    public void Select_WithPaging_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int pageSize = 10;
        int pageOffset = 20;

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
            FROM {{p}}
            ORDER BY {{p[x => x.Id]}}
            {{Sql.Paging(pageSize, pageOffset)}}
            """)).Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> AllDialectsPagingData =>
    [   new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
                FROM <<dbo>>.<<Products>>
                ORDER BY <<dbo>>.<<Products>>.<<Id>>
                LIMIT 10 OFFSET 20
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                ORDER BY `dbo`.`Products`.`Id`
                LIMIT 10 OFFSET 20
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                ORDER BY "dbo"."Products"."Id"
                OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                ORDER BY "dbo"."Products"."Id"
                LIMIT 10 OFFSET 20
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                ORDER BY "dbo"."Products"."Id"
                LIMIT 10 OFFSET 20
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products]
                ORDER BY [dbo].[Products].[Id]
                OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
                """
            ]
        ),
    ];
}