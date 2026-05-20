using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class InsertSubqueryTests
{
    [Theory]
    [MemberData(nameof(InsertSelectData))]
    public void Insert_WithSelectSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var targetId = 100;
        
        // Act
        // Insert into OrderModel (Total) using OrderLine (Quantity)
        var result = db.Query<OrderModel, OrderLine>((o, l) =>
            db.Append($$"""
            INSERT INTO {{o}} 
            ({{o[x => x.Id]}}, {{o[x => x.Total]}})
            SELECT {{l[x => x.OrderId]}}, {{l[x => x.Quantity]}}
            FROM {{l}}
            WHERE {{l[x => x.OrderId]}} = {{targetId}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
        
        // Ensure the parameter was captured sequentially
        Assert.Equal(targetId, result.Parameters.ElementAt(0).Value);
    }

    public static TheoryData<SqlTestCase> InsertSelectData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                INSERT INTO <<dbo>>.<<Orders>> 
                (<<Id>>, <<Total>>)
                SELECT <<OrderLine>>.<<OrderId>>, <<OrderLine>>.<<Quantity>>
                FROM <<OrderLine>>
                WHERE <<OrderLine>>.<<OrderId>> = !!100
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                INSERT INTO "dbo"."Orders" 
                ("Id", "Total")
                SELECT "OrderLine"."OrderId", "OrderLine"."Quantity"
                FROM "OrderLine"
                WHERE "OrderLine"."OrderId" = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Orders` 
                (`Id`, `Total`)
                SELECT `OrderLine`.`OrderId`, `OrderLine`.`Quantity`
                FROM `OrderLine`
                WHERE `OrderLine`.`OrderId` = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                INSERT INTO "dbo"."Orders" 
                ("Id", "Total")
                SELECT "OrderLine"."OrderId", "OrderLine"."Quantity"
                FROM "OrderLine"
                WHERE "OrderLine"."OrderId" = :0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                INSERT INTO "dbo"."Orders" 
                ("Id", "Total")
                SELECT "OrderLine"."OrderId", "OrderLine"."Quantity"
                FROM "OrderLine"
                WHERE "OrderLine"."OrderId" = $1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                INSERT INTO "dbo"."Orders" 
                ("Id", "Total")
                SELECT "OrderLine"."OrderId", "OrderLine"."Quantity"
                FROM "OrderLine"
                WHERE "OrderLine"."OrderId" = ?0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                INSERT INTO [dbo].[Orders] 
                ([Id], [Total])
                SELECT [OrderLine].[OrderId], [OrderLine].[Quantity]
                FROM [OrderLine]
                WHERE [OrderLine].[OrderId] = @p0
                """
            ]
        )
    ];
}