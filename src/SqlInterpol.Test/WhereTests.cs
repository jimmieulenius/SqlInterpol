using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class WhereTests
{
    [Theory]
    [MemberData(nameof(WhereSimpleParameterData))]
    public void Where_SimpleParameter_ShouldCaptureCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int targetId = 42;

        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}}
            WHERE {{p[x => x.Id]}} = {{targetId}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
        Assert.Single(result.Parameters);
        Assert.Equal(42, result.Parameters.Values.First());
    }

    public static TheoryData<SqlTestCase> WhereSimpleParameterData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<dbo>>.<<Products>>.<<Id>>
            FROM <<dbo>>.<<Products>>
            WHERE <<dbo>>.<<Products>>.<<Id>> = !!100
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `dbo`.`Products`.`Id`
            FROM `dbo`.`Products`
            WHERE `dbo`.`Products`.`Id` = @p0
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."Id" = :0
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."Id" = $1
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."Id" = ?0
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [dbo].[Products].[Id]
            FROM [dbo].[Products]
            WHERE [dbo].[Products].[Id] = @p0
            """
        )
    ];

    [Theory]
    [MemberData(nameof(WhereInCollectionData))]
    public void Where_InCollection_ShouldExpandToCommaSeparatedParameters(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int[] categoryIds = [10, 20, 30];

        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}}
            WHERE {{p[x => x.CategoryId]}} IN ({{categoryIds}})
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
        Assert.Equal(3, result.Parameters.Count);
        
        var paramValues = result.Parameters.Values.ToList();
        Assert.Equal(10, paramValues[0]);
        Assert.Equal(20, paramValues[1]);
        Assert.Equal(30, paramValues[2]);
    }

    public static TheoryData<SqlTestCase> WhereInCollectionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            """
            SELECT
                <<dbo>>.<<Products>>.<<Id>>
            FROM <<dbo>>.<<Products>>
            WHERE <<dbo>>.<<Products>>.<<CategoryId>> IN (!!100, !!101, !!102)
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            """
            SELECT
                `dbo`.`Products`.`Id`
            FROM `dbo`.`Products`
            WHERE `dbo`.`Products`.`CategoryId` IN (@p0, @p1, @p2)
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."CategoryId" IN (:0, :1, :2)
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."CategoryId" IN ($1, $2, $3)
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT
                "dbo"."Products"."Id"
            FROM "dbo"."Products"
            WHERE "dbo"."Products"."CategoryId" IN (?0, ?1, ?2)
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            """
            SELECT
                [dbo].[Products].[Id]
            FROM [dbo].[Products]
            WHERE [dbo].[Products].[CategoryId] IN (@p0, @p1, @p2)
            """
        )
    ];
}