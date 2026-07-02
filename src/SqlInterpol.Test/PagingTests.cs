using System;
using System.Linq;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class PagingTests
{
    [Theory]
    [MemberData(nameof(Paging_WithImplicitLimitOffsetData))]
    public void Paging_WithImplicitLimitOffset(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int pageSize = 10;
        int pageOffset = 20;

        // Act - Using standard PostgreSQL/MySQL syntax. The engine transpiles it for others!
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}, {{p.Name}}
            FROM {{p}}
            ORDER BY {{p.Id}}
            LIMIT {{pageSize}} OFFSET {{pageOffset}}
            """)
            .Build()
        );

        // Assert - Natively verifies the SQL string AND the expected parameters array!
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> Paging_WithImplicitLimitOffsetData
    {
        get
        {
            object?[] expectedParams = [10, 20];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
                        FROM <<dbo>>.<<Products>>
                        ORDER BY <<dbo>>.<<Products>>.<<Id>>
                        LIMIT !!100 OFFSET !!101
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        ORDER BY "dbo"."Products"."Id"
                        FIRST @p0 SKIP @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                        FROM `dbo`.`Products`
                        ORDER BY `dbo`.`Products`.`Id`
                        LIMIT @p0 OFFSET @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                // Oracle (Swaps the visual order and applies ANSI syntax)
                // Notice how :1 comes before :0 in the SQL string, but the params array remains [10, 20]!
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        ORDER BY "dbo"."Products"."Id"
                        OFFSET :1 ROWS FETCH NEXT :0 ROWS ONLY
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        ORDER BY "dbo"."Products"."Id"
                        LIMIT $1 OFFSET $2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        ORDER BY "dbo"."Products"."Id"
                        LIMIT @p1 OFFSET @p2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                // SQL Server (Swaps the visual order and applies ANSI syntax)
                // Notice how @p1 comes before @p0 in the SQL string!
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                        FROM [dbo].[Products]
                        ORDER BY [dbo].[Products].[Id]
                        OFFSET @p1 ROWS FETCH NEXT @p0 ROWS ONLY
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}