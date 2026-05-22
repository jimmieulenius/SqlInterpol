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

    [Theory]
    [MemberData(nameof(DeleteMultiTableData))]
    public void Delete_MultiTable_TranslatesAcrossDialects(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Pure dialect-agnostic WYSIWYG!
        var result = db
            .Entity<Product>()
            .Entity<Category>()
            .Query((p, c) => db.Append($$"""
                DELETE FROM {{p}}
                FROM {{c}} AS c1
                WHERE {{p[x => x.CategoryId]}} = c1.Id
                """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
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
                WHERE "dbo"."Orders"."Id" = @p1
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

    public static TheoryData<SqlTestCase> DeleteMultiTableData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                DELETE FROM <<dbo>>.<<Products>>
                FROM <<Category>> AS c1
                WHERE <<dbo>>.<<Products>>.<<CategoryId>> = c1.Id
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                // MySQL accurately extracts the target table to the front of the FROM clause
                """
                DELETE `dbo`.`Products`
                FROM `dbo`.`Products`, `Category` AS c1
                WHERE `dbo`.`Products`.`CategoryId` = c1.Id
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                // Oracle flawlessly transforms the entire AST into an EXISTS subquery block!
                """
                DELETE FROM "dbo"."Products"
                WHERE EXISTS (
                    SELECT 1
                    FROM "Category" c1
                    WHERE "dbo"."Products"."CategoryId" = c1.Id
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                // Postgres perfectly maps the second FROM to USING
                """
                DELETE FROM "dbo"."Products"
                USING "Category" AS c1
                WHERE "dbo"."Products"."CategoryId" = c1.Id
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                DELETE FROM "dbo"."Products"
                FROM "Category" AS c1
                WHERE "dbo"."Products"."CategoryId" = c1.Id
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                DELETE FROM [dbo].[Products]
                FROM [Category] AS c1
                WHERE [dbo].[Products].[CategoryId] = c1.Id
                """
            ]
        )
    ];
}