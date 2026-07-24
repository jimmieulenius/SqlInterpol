using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class SetOperationTests
{
    [Theory]
    [MemberData(nameof(QueryIntersectData))]
    public void Intersect(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
                SELECT {{p.Id}} FROM {{p}}
                INTERSECT
                SELECT {{p.Id}} FROM {{p}} WHERE {{p.CategoryId}} = {{1}}
                """)
            .Build()
        );

        // Assert - Natively verifies the SQL string AND the hoisted parameter!
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(QueryIntersectData))]
    public void Intersect_Query(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // We only declare the entity scope ONCE for the entire test!
        db.Entity<Product>(out var p);
        
        // Build isolated modular fragments using the shared scope
        var q1 = db.Fragment($"SELECT {p.Id} FROM {p}");
        var q2 = db.Fragment($"SELECT {p.Id} FROM {p} WHERE {p.CategoryId} = {1}");

        // Act
        testCase.Action(() => db.Append($$"""
            {{q1}}
            INTERSECT
            {{q2}}
            """).Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(QueryExceptData))]
    public void Except(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
                SELECT {{p.Id}} FROM {{p}}
                EXCEPT
                SELECT {{p.Id}} FROM {{p}} WHERE {{p.CategoryId}} = {{2}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(QueryExceptData))]
    public void Except_Query(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Entity<Product>(out var p);
        
        // Build isolated modular fragments
        var q1 = db.Fragment($"SELECT {p.Id} FROM {p}");
        var q2 = db.Fragment($"SELECT {p.Id} FROM {p} WHERE {p.CategoryId} = {2}");

        // Act
        testCase.Action(() => db.Append($$"""
            {{q1}}
            EXCEPT
            {{q2}}
            """).Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(Select_UnionAllData))]
    public void Select_UnionAll(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        int cat1 = 1;
        int cat2 = 2;

        db.Entity<Product>(out var p);

        // Build the first query
        var query1 = db.Fragment($$"""
            SELECT {{p.Id}}, {{p.Name}}
            FROM {{p}}
            WHERE {{p.CategoryId}} = {{cat1}}
            """);

        // Build the second query
        var query2 = db.Fragment($$"""
            SELECT {{p.Id}}, {{p.Name}}
            FROM {{p}}
            WHERE {{p.CategoryId}} = {{cat2}}
            """);

        // Act - Pure WYSIWYG Union!
        testCase.Action(() => db.Append($$"""
            {{query1}}
            UNION ALL
            {{query2}}
            """).Build()
        );

        // Assert - Proof that the context successfully shared the parameter counter across nested builders!
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> QueryIntersectData
    {
        get
        {
            object?[] expectedParams = [1];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>>
                        INTERSECT
                        SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>> WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT `dbo`.`Products`.`Id` FROM `dbo`.`Products`
                        INTERSECT
                        SELECT `dbo`.`Products`.`Id` FROM `dbo`.`Products` WHERE `dbo`.`Products`.`CategoryId` = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products"
                        INTERSECT
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = :0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products"
                        INTERSECT
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = $1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products"
                        INTERSECT
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT [dbo].[Products].[Id] FROM [dbo].[Products]
                        INTERSECT
                        SELECT [dbo].[Products].[Id] FROM [dbo].[Products] WHERE [dbo].[Products].[CategoryId] = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> QueryExceptData
    {
        get
        {
            object?[] expectedParams = [2];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>>
                        EXCEPT
                        SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>> WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT `dbo`.`Products`.`Id` FROM `dbo`.`Products`
                        EXCEPT
                        SELECT `dbo`.`Products`.`Id` FROM `dbo`.`Products` WHERE `dbo`.`Products`.`CategoryId` = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                // CRITICAL CHECK: Ensure Oracle correctly overrides EXCEPT to MINUS (Validated by lexer!)
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products"
                        MINUS
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = :0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products"
                        EXCEPT
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = $1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products"
                        EXCEPT
                        SELECT "dbo"."Products"."Id" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT [dbo].[Products].[Id] FROM [dbo].[Products]
                        EXCEPT
                        SELECT [dbo].[Products].[Id] FROM [dbo].[Products] WHERE [dbo].[Products].[CategoryId] = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> Select_UnionAllData
    {
        get
        {
            object?[] expectedParams = [1, 2];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
                        FROM <<dbo>>.<<Products>>
                        WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100
                        UNION ALL
                        SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
                        FROM <<dbo>>.<<Products>>
                        WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!101
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
                        WHERE `dbo`.`Products`.`CategoryId` = @p0
                        UNION ALL
                        SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                        FROM `dbo`.`Products`
                        WHERE `dbo`.`Products`.`CategoryId` = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."CategoryId" = :0
                        UNION ALL
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."CategoryId" = :1
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
                        WHERE "dbo"."Products"."CategoryId" = $1
                        UNION ALL
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."CategoryId" = $2
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
                        WHERE "dbo"."Products"."CategoryId" = @p1
                        UNION ALL
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."CategoryId" = @p2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                        FROM [dbo].[Products]
                        WHERE [dbo].[Products].[CategoryId] = @p0
                        UNION ALL
                        SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                        FROM [dbo].[Products]
                        WHERE [dbo].[Products].[CategoryId] = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}