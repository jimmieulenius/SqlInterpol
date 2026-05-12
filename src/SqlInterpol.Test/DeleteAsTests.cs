using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class DeleteAsTests
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
    [MemberData(nameof(SqlServerDeleteAsData))]
    public void Delete_WithAlias_SqlServerSyntax_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var targetId = 42;

        // Act
        // SQL Server requires: DELETE [alias] FROM [table] AS [alias]
        var result = db.Query<OrderModel>(o => 
            db.Append($$""" 
            DELETE {{o.Reference}}
            FROM {{o}} AS {{Sql.Quote("o")}}
            WHERE {{o[x => x.Id]}} = {{targetId}}
            """)) 
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        Assert.Equal(targetId, result.Parameters.ElementAt(0).Value);
    }

    // We only test SqlServerDialect here because this is specific SQL Server syntax
    public static TheoryData<SqlTestCase> SqlServerDeleteAsData =>
    [
        new SqlTestCase(SqlDialectKind.SqlServer, [
            """
            DELETE [o]
            FROM [dbo].[Orders] AS [o]
            WHERE [o].[Id] = @p0
            """
        ])
    ];

    [Theory]
    [MemberData(nameof(PostgresDeleteAsData))]
    public void Delete_WithAlias_PostgresSyntax_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var targetId = 42;

        // Act
        // Postgres allows: DELETE FROM [table] AS [alias]
        var result = db.Entity<OrderModel>(alias: "o").Query(o =>
            db.Append($$"""
            DELETE FROM {{o.Declaration}}
            WHERE {{o[x => x.Id]}} = {{targetId}}
            """
            ))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        Assert.Equal(targetId, result.Parameters.ElementAt(0).Value);
    }

    // We only test PostgresDialect here because this is specific Postgres syntax
    public static TheoryData<SqlTestCase> PostgresDeleteAsData =>
    [
        new SqlTestCase(SqlDialectKind.PostgreSql, [
            """
            DELETE FROM "dbo"."Orders" AS "o"
            WHERE "o"."Id" = $1
            """
        ])
    ];
}