using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class DeleteTests
{
    [SqlTable("Orders", Schema = "dbo")]
    public record OrderModel
    {
        public int Id { get; init; }

        [SqlColumn("order_status")]
        public string Status { get; init; } = "";
        
        public decimal Total { get; init; }
    }

    [Theory]
    [MemberData(nameof(DeleteData))]
    public void Delete_PureManual_RendersCorrectly(SqlTestCase testCase)
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
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        Assert.Equal(targetId, result.Parameters.ElementAt(0).Value);
    }

    public static TheoryData<SqlTestCase> DeleteData =>
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