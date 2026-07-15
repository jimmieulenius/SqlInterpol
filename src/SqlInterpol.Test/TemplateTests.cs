using System.Collections.Concurrent;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class TemplateTests
{
    // Local lightweight payloads used to guarantee reflection order determinism
    public class OrderIdPayload
    {
        public int Id { get; set; }
    }

    public class OrderUpdatePayload
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
    }

    public class OrderDeletePayload
    {
        public int Id { get; set; }
    }

    public class OrderInsertPayload
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
    }

    private static readonly ConcurrentDictionary<SqlDialectKind, ISqlTemplate> _activeOrderTemplates = new();

    private static ISqlTemplate CompileOrderTemplate(ISqlDialect dialect)
    {
        var db = new SqlBuilder(dialect);
        db.Entity<OrderModel>(out var o);
        
        db.Template(out var template, $$"""
            SELECT {{o.Id}}, {{o.CustomerId}}
            FROM {{o}} AS o1
            WHERE {{o.CustomerId}} = {{Sql.Arg("CustId")}}
            """);
            
        return template;
    }

    [Theory]
    [MemberData(nameof(TemplateSelectData))]
    public void Template_Select(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            
            db.Entity<OrderModel>(out var o, "o1"); 

            var activeOrderTemplate = _activeOrderTemplates.GetOrAdd(
                db.Context.Dialect.Kind, 
                kind => CompileOrderTemplate(db.Context.Dialect));

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

    [Theory]
    [MemberData(nameof(TemplateUpdateData))]
    public void Template_Update(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            db.Entity<OrderModel>(out var o);

            var payloads = new[]
            {
                new OrderUpdatePayload { Id = 101, CustomerId = 500 },
                new OrderUpdatePayload { Id = 102, CustomerId = 501 }
            };

            // Executes batched updates using the target entity property mapping
            return db.AppendUpdate(o, o.Id, payloads).Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(TemplateDeleteData))]
    public void Template_Delete(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            db.Entity<OrderModel>(out var o);

            var payloads = new[]
            {
                new OrderDeletePayload { Id = 101 },
                new OrderDeletePayload { Id = 102 }
            };

            // Executes batched deletes using the target entity property mapping
            return db.AppendDelete(o, o.Id, payloads).Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(TemplateManualInsertData))]
    public void Template_ManualInsert(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            db.Entity<OrderModel>(out var o);

            // The macro dynamically expands into (Col1, Col2) VALUES ({0}, {1}) and maps the args natively!
            db.Template(out var manualInsertTemplate, $$"""
                INSERT INTO {{o}}
                VALUES {{Sql.Expand<OrderInsertPayload>()}}
                """);

            return db.Append(manualInsertTemplate, new OrderInsertPayload { Id = 101, CustomerId = 5 }).Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(TemplateManualUpdateData))]
    public void Template_ManualUpdate(SqlTestCase testCase)
    {
        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            db.Entity<OrderModel>(out var o);

            // Passing "Id" excludes it from the SET list, allowing us to map it safely in the WHERE clause!
            db.Template(out var manualUpdateTemplate, $$"""
                UPDATE {{o}}
                SET {{Sql.Expand<OrderUpdatePayload>("Id")}}
                WHERE {{o.Id:col}} = {{Sql.Arg("Id")}}
                """);

            return db.Append(manualUpdateTemplate, new OrderUpdatePayload { Id = 101, CustomerId = 500 }).Build();
        });

        testCase.Assert();
    }

    // =========================================================================
    // THEORY DATA
    // =========================================================================

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

    public static TheoryData<SqlTestCase> TemplateUpdateData =>
        [
            new SqlTestCase(
                SqlDialectKind.CustomDb,
                [
                    """
                    UPDATE <<dbo>>.<<Orders>> SET <<CustomerId>> = !!100 WHERE <<Id>> = !!101;
                    UPDATE <<dbo>>.<<Orders>> SET <<CustomerId>> = !!102 WHERE <<Id>> = !!103
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Firebird,
                [
                    """
                    UPDATE "dbo"."Orders" SET "CustomerId" = @p0 WHERE "Id" = @p1;
                    UPDATE "dbo"."Orders" SET "CustomerId" = @p2 WHERE "Id" = @p3
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.MySql,
                [
                    """
                    UPDATE `dbo`.`Orders` SET `CustomerId` = @p0 WHERE `Id` = @p1;
                    UPDATE `dbo`.`Orders` SET `CustomerId` = @p2 WHERE `Id` = @p3
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Oracle,
                [
                    """
                    UPDATE "dbo"."Orders" SET "CustomerId" = :0 WHERE "Id" = :1;
                    UPDATE "dbo"."Orders" SET "CustomerId" = :2 WHERE "Id" = :3
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.PostgreSql,
                [
                    """
                    UPDATE "dbo"."Orders" SET "CustomerId" = $1 WHERE "Id" = $2;
                    UPDATE "dbo"."Orders" SET "CustomerId" = $3 WHERE "Id" = $4
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqLite,
                [
                    """
                    UPDATE "dbo"."Orders" SET "CustomerId" = @p1 WHERE "Id" = @p2;
                    UPDATE "dbo"."Orders" SET "CustomerId" = @p3 WHERE "Id" = @p4
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqlServer,
                [
                    """
                    UPDATE [dbo].[Orders] SET [CustomerId] = @p0 WHERE [Id] = @p1;
                    UPDATE [dbo].[Orders] SET [CustomerId] = @p2 WHERE [Id] = @p3
                    """
                ]
            )
        ];

    public static TheoryData<SqlTestCase> TemplateDeleteData =>
        [
            new SqlTestCase(
                SqlDialectKind.CustomDb,
                [
                    """
                    DELETE FROM <<dbo>>.<<Orders>> WHERE <<Id>> = !!100;
                    DELETE FROM <<dbo>>.<<Orders>> WHERE <<Id>> = !!101
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Firebird,
                [
                    """
                    DELETE FROM "dbo"."Orders" WHERE "Id" = @p0;
                    DELETE FROM "dbo"."Orders" WHERE "Id" = @p1
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.MySql,
                [
                    """
                    DELETE FROM `dbo`.`Orders` WHERE `Id` = @p0;
                    DELETE FROM `dbo`.`Orders` WHERE `Id` = @p1
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Oracle,
                [
                    """
                    DELETE FROM "dbo"."Orders" WHERE "Id" = :0;
                    DELETE FROM "dbo"."Orders" WHERE "Id" = :1
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.PostgreSql,
                [
                    """
                    DELETE FROM "dbo"."Orders" WHERE "Id" = $1;
                    DELETE FROM "dbo"."Orders" WHERE "Id" = $2
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqLite,
                [
                    """
                    DELETE FROM "dbo"."Orders" WHERE "Id" = @p1;
                    DELETE FROM "dbo"."Orders" WHERE "Id" = @p2
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqlServer,
                [
                    """
                    DELETE FROM [dbo].[Orders] WHERE [Id] = @p0;
                    DELETE FROM [dbo].[Orders] WHERE [Id] = @p1
                    """
                ]
            )
        ];

    public static TheoryData<SqlTestCase> TemplateManualInsertData =>
        [
            new SqlTestCase(
                SqlDialectKind.CustomDb,
                [
                    """
                    INSERT INTO <<dbo>>.<<Orders>>
                    (<<Id>>, <<CustomerId>>)
                    VALUES (!!100, !!101)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Firebird,
                [
                    """
                    INSERT INTO "dbo"."Orders"
                    ("Id", "CustomerId")
                    VALUES (@p0, @p1)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.MySql,
                [
                    """
                    INSERT INTO `dbo`.`Orders`
                    (`Id`, `CustomerId`)
                    VALUES (@p0, @p1)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Oracle,
                [
                    """
                    INSERT INTO "dbo"."Orders"
                    ("Id", "CustomerId")
                    VALUES (:0, :1)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.PostgreSql,
                [
                    """
                    INSERT INTO "dbo"."Orders"
                    ("Id", "CustomerId")
                    VALUES ($1, $2)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqLite,
                [
                    """
                    INSERT INTO "dbo"."Orders"
                    ("Id", "CustomerId")
                    VALUES (@p1, @p2)
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqlServer,
                [
                    """
                    INSERT INTO [dbo].[Orders]
                    ([Id], [CustomerId])
                    VALUES (@p0, @p1)
                    """
                ]
            )
        ];

    public static TheoryData<SqlTestCase> TemplateManualUpdateData =>
        [
            new SqlTestCase(
                SqlDialectKind.CustomDb,
                [
                    """
                    UPDATE <<dbo>>.<<Orders>>
                    SET <<CustomerId>> = !!100
                    WHERE <<Id>> = !!101
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Firebird,
                [
                    """
                    UPDATE "dbo"."Orders"
                    SET "CustomerId" = @p0
                    WHERE "Id" = @p1
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.MySql,
                [
                    """
                    UPDATE `dbo`.`Orders`
                    SET `CustomerId` = @p0
                    WHERE `Id` = @p1
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.Oracle,
                [
                    """
                    UPDATE "dbo"."Orders"
                    SET "CustomerId" = :0
                    WHERE "Id" = :1
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.PostgreSql,
                [
                    """
                    UPDATE "dbo"."Orders"
                    SET "CustomerId" = $1
                    WHERE "Id" = $2
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqLite,
                [
                    """
                    UPDATE "dbo"."Orders"
                    SET "CustomerId" = @p1
                    WHERE "Id" = @p2
                    """
                ]
            ),
            new SqlTestCase(
                SqlDialectKind.SqlServer,
                [
                    """
                    UPDATE [dbo].[Orders]
                    SET [CustomerId] = @p0
                    WHERE [Id] = @p1
                    """
                ]
            )
        ];
}