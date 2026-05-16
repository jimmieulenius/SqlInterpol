using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class UnionTests
{
    [Theory]
    [MemberData(nameof(Select_UnionAllData))]
    public void Select_UnionAll(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        int cat1 = 1;
        int cat2 = 2;

        // Build the first query
        var query1 = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
            FROM {{p}}
            WHERE {{p[x => x.CategoryId]}} = {{cat1}}
            """));

        // Build the second query
        var query2 = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
            FROM {{p}}
            WHERE {{p[x => x.CategoryId]}} = {{cat2}}
            """));

        // Act - Pure WYSIWYG Union!
        var result = db.Append($$"""
            {{query1}}
            UNION ALL
            {{query2}}
            """).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);

        // Assert Parameters - Proof that the context successfully shared the parameter counter!
        Assert.Equal(2, result.Parameters.Count);
        
        // Asserting parameter values via index since parameter names vary by dialect (e.g. @p0 vs @p1 vs $1)
        var parametersList = result.Parameters.Select(p => p.Value).ToList();
        Assert.Equal(1, parametersList[0]);
        Assert.Equal(2, parametersList[1]);
    }

    public static TheoryData<SqlTestCase> Select_UnionAllData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
                FROM <<dbo>>.<<Products>>
                WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100
                UNION ALL
                SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
                FROM <<dbo>>.<<Products>>
                WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!101
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`CategoryId` = @p0
                UNION ALL
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`CategoryId` = @p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."CategoryId" = :0
                UNION ALL
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."CategoryId" = :1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."CategoryId" = $1
                UNION ALL
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."CategoryId" = $2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."CategoryId" = ?0
                UNION ALL
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."CategoryId" = ?1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products]
                WHERE [dbo].[Products].[CategoryId] = @p0
                UNION ALL
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products]
                WHERE [dbo].[Products].[CategoryId] = @p1
                """
            ]
        )
    ];
}