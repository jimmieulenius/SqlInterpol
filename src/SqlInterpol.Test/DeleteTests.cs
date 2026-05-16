using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class DeleteTests
{
    [Theory]
    [MemberData(nameof(DeletePureManualData))]
    public void Delete_PureManual(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var targetId = 42;

        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            DELETE FROM {{o}}
            WHERE {{o[x => x.Id]}} = {{targetId}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Equal(targetId, result.Parameters.ElementAt(0).Value);
    }

    public static TheoryData<SqlTestCase> DeletePureManualData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                DELETE FROM <<dbo>>.<<Orders>>
                WHERE <<dbo>>.<<Orders>>.<<Id>> = !!100
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                DELETE FROM `dbo`.`Orders`
                WHERE `dbo`.`Orders`.`Id` = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                DELETE FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" = :0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                DELETE FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" = $1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                DELETE FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" = ?0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                DELETE FROM [dbo].[Orders]
                WHERE [dbo].[Orders].[Id] = @p0
                """
            ]
        )
    ];
}