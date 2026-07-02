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
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Entity<OrderLine>(out var l)
            .Append($$"""
                INSERT INTO {{o}} 
                ({{o.Id}}, {{o.Total}})
                SELECT {{l.OrderId}}, {{l.Quantity}}
                FROM {{l}}
                WHERE {{l.OrderId}} = {{targetId}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> InsertSelectData
    {
        get
        {
            object?[] expectedParams = [100];

            return
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
                    ],
                    expectedParameters: expectedParams
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
                    ],
                    expectedParameters: expectedParams
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
                    ],
                    expectedParameters: expectedParams
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
                    ],
                    expectedParameters: expectedParams
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
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        INSERT INTO "dbo"."Orders" 
                        ("Id", "Total")
                        SELECT "OrderLine"."OrderId", "OrderLine"."Quantity"
                        FROM "OrderLine"
                        WHERE "OrderLine"."OrderId" = @p1
                        """
                    ],
                    expectedParameters: expectedParams
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
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}