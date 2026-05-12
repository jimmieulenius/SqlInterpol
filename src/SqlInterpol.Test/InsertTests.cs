using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class InsertTests
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
    [MemberData(nameof(InsertData))]
    public void Insert_WithDto_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newOrder = new { Status = "New", Total = 10.50m };
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($"{Sql.Insert(o, newOrder)}"))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        
        // Verify sequential parameter order (@p0 = Status, @p1 = Total)
        Assert.Equal("New", result.Parameters.ElementAt(0).Value);
        Assert.Equal(10.50m, result.Parameters.ElementAt(1).Value);
    }

    [Theory]
    [MemberData(nameof(InsertData))]
    public void Insert_WithExplicitSets_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($"{Sql.Insert(o,
                Sql.Set(o[x => x.Status], "New"),
                Sql.Set(o[x => x.Total], 10.50m)
            )}"))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    [Theory]
    [MemberData(nameof(InsertData))]
    public void Insert_PureManual_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var status = "Manual";
        var total = 100.00m;
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            INSERT INTO {{o}}
            ({{o[x => x.Status]}}, {{o[x => x.Total]}})
            VALUES ({{status}}, {{total}})
            """))
            .Build();

        // Assert
        // We expect the exact same SQL as the macro version!
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        
        // Verify parameters
        Assert.Equal(status, result.Parameters.ElementAt(0).Value);
        Assert.Equal(total, result.Parameters.ElementAt(1).Value);
    }

    public static TheoryData<SqlTestCase> InsertData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                INSERT INTO <<dbo>>.<<Orders>>
                (<<order_status>>, <<Total>>)
                VALUES (!!100, !!101)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Orders`
                (`order_status`, `Total`)
                VALUES (@p0, @p1)
                """
            ]
        ),
        new SqlTestCase(SqlDialectKind.Oracle, [
            """
            INSERT INTO "dbo"."Orders"
            ("order_status", "Total")
            VALUES (:0, :1)
            """
        ]),
        new SqlTestCase(SqlDialectKind.PostgreSql, [
            """
            INSERT INTO "dbo"."Orders"
            ("order_status", "Total")
            VALUES ($1, $2)
            """
        ]),
        new SqlTestCase(SqlDialectKind.SqLite, [
            """
            INSERT INTO "dbo"."Orders"
            ("order_status", "Total")
            VALUES (?0, ?1)
            """
        ]),
        new SqlTestCase(SqlDialectKind.SqlServer, [
            """
            INSERT INTO [dbo].[Orders]
            ([order_status], [Total])
            VALUES (@p0, @p1)
            """
        ])
    ];

    [Theory]
    [MemberData(nameof(InsertValuesData))]
    public void InsertValues_ManualStatement_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var dto = new { Status = "Active", Total = 50.00m };
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            INSERT INTO {{o}} 
            {{Sql.InsertValues(o, dto)}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> InsertValuesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, [
                """
                INSERT INTO <<dbo>>.<<Orders>> 
                (<<order_status>>, <<Total>>)
                VALUES (!!100, !!101)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, [
                """
                INSERT INTO `dbo`.`Orders` 
                (`order_status`, `Total`)
                VALUES (@p0, @p1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, [
                """
                INSERT INTO "dbo"."Orders" 
                ("order_status", "Total")
                VALUES (:0, :1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, [
                """
                INSERT INTO "dbo"."Orders" 
                ("order_status", "Total")
                VALUES ($1, $2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, [
                """
                INSERT INTO "dbo"."Orders" 
                ("order_status", "Total")
                VALUES (?0, ?1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, [
                """
                INSERT INTO [dbo].[Orders] 
                ([order_status], [Total])
                VALUES (@p0, @p1)
                """
            ]
        )
    ];
}