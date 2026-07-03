using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class TemplateTests
{
    // Local lightweight payload used to guarantee reflection order determinism
    public class OrderIdPayload
    {
        public int Id { get; set; }
    }

    [Theory]
    [MemberData(nameof(TemplateSelectData))]
    public void Template_Select(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            db.Entity<OrderModel>(out var o);

            // 1. Compile the template ahead of time (Zero AST allocations on subsequent executions)
            db.Template(out var activeOrderTemplate, $$"""
                SELECT {{o.Id}}, {{o.CustomerId}}
                FROM {{o}} AS o1
                WHERE {{o.CustomerId}} = {{Sql.Arg("CustId")}}
                """);

            // 2. Render and dynamically compose around the template
            return db.Append(activeOrderTemplate, new { CustId = 5 })
                     .AppendLine()
                     .Append($"ORDER BY {o.Id} DESC")
                     .Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(TemplateBulkInsertData))]
    public void Template_BulkInsert(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            db.Entity<OrderModel>(out var o);

            var payloads = new[]
            {
                new OrderIdPayload { Id = 101 },
                new OrderIdPayload { Id = 102 }
            };

            // Executes via the unified, single-allocation cached params Bulk Insert template
            return db.AppendInsert(o, payloads).Build();
        });

        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> TemplateSelectData =>
        [
            new SqlTestCase(
                SqlDialectKind.CustomDb,
                [
                    """
                    SELECT <<o1>>.<<Id>>, <<o1>>.<<CustomerId>>
                    FROM <<dbo>>.<<Orders>> AS <<o1>>
                    WHERE <<o1>>.<<CustomerId>> = !!100
                    ORDER BY <<o1>>.<<Id>> DESC
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Firebird,
                [
                    """
                    SELECT "o1"."Id", "o1"."CustomerId"
                    FROM "dbo"."Orders" AS "o1"
                    WHERE "o1"."CustomerId" = @p0
                    ORDER BY "o1"."Id" DESC
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.MySql,
                [
                    """
                    SELECT `o1`.`Id`, `o1`.`CustomerId`
                    FROM `dbo`.`Orders` AS `o1`
                    WHERE `o1`.`CustomerId` = @p0
                    ORDER BY `o1`.`Id` DESC
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Oracle,
                [
                    """
                    SELECT "o1"."Id", "o1"."CustomerId"
                    FROM "dbo"."Orders" "o1"
                    WHERE "o1"."CustomerId" = :0
                    ORDER BY "o1"."Id" DESC
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.PostgreSql,
                [
                    """
                    SELECT "o1"."Id", "o1"."CustomerId"
                    FROM "dbo"."Orders" AS "o1"
                    WHERE "o1"."CustomerId" = $1
                    ORDER BY "o1"."Id" DESC
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqLite,
                [
                    """
                    SELECT "o1"."Id", "o1"."CustomerId"
                    FROM "dbo"."Orders" AS "o1"
                    WHERE "o1"."CustomerId" = @p1
                    ORDER BY "o1"."Id" DESC
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqlServer,
                [
                    """
                    SELECT [o1].[Id], [o1].[CustomerId]
                    FROM [dbo].[Orders] AS [o1]
                    WHERE [o1].[CustomerId] = @p0
                    ORDER BY [o1].[Id] DESC
                    """
                ]
            )
        ];

    public static TheoryData<SqlTestCase> TemplateBulkInsertData =>
        [

            new SqlTestCase(
                SqlDialectKind.CustomDb,
                [
                    """
                    INSERT INTO <<dbo>>.<<Orders>> (<<Id>>)
                    VALUES (!!100), (!!101)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Firebird,
                [
                    """
                    INSERT INTO "dbo"."Orders" ("Id")
                    VALUES (@p0), (@p1)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.MySql,
                [
                    """
                    INSERT INTO `dbo`.`Orders` (`Id`)
                    VALUES (@p0), (@p1)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Oracle,
                [
                    """
                    INSERT INTO "dbo"."Orders" ("Id")
                    VALUES (:0), (:1)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.PostgreSql,
                [
                    """
                    INSERT INTO "dbo"."Orders" ("Id")
                    VALUES ($1), ($2)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqLite,
                [
                    """
                    INSERT INTO "dbo"."Orders" ("Id")
                    VALUES (@p1), (@p2)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqlServer,
                [
                    """
                    INSERT INTO [dbo].[Orders] ([Id])
                    VALUES (@p0), (@p1)
                    """
                ]
            )
        ];
}