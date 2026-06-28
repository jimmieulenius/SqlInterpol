using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class DeleteTests
{
    // Shared test data at the class level ensures zero drift between execution and assertions!
    private const int TargetId = 42;
    private static readonly TestUser TemplateUser = new() { Id = 1, Name = "Bob", Age = 31 };

    [Theory]
    [MemberData(nameof(DeletePureManualData))]
    public void Delete_PureManual(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$"""
                DELETE FROM {{o}}
                WHERE {{o.Id}} = {{TargetId}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(DeleteMultiTableData))]
    public void Delete_MultiTable_TranslatesAcrossDialects(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Pure dialect-agnostic WYSIWYG!
        testCase.Action(() => db
            .Entity<Product>(out var p)
            .Entity<Category>(out var c)
            .Append($$"""
                DELETE FROM {{p}}
                FROM {{c}} AS c1
                WHERE {{p.CategoryId}} = c1.Id
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    // TODO: Update template
    // [Theory]
    // [MemberData(nameof(DeleteTemplateData))]
    // public void AppendDelete_Template(SqlTestCase testCase)
    // {
    //     // Arrange
    //     var db = testCase.CreateBuilder();
    //     var user = new TestUser { Id = 1, Name = "Bob", Age = 31 };

    //     // Act
    //     testCase.Action(() => db.Query<TestUser>(u => 
    //         db.AppendDelete(u, user, x => x.Id)
    //     ).Build()
    //     );

    //     // Assert
    //     testCase.Assert();
    // }

    public static TheoryData<SqlTestCase> DeletePureManualData
    {
        get
        {
            object?[] expectedParams = [TargetId];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        DELETE FROM <<dbo>>.<<Orders>>
                        WHERE <<dbo>>.<<Orders>>.<<Id>> = !!100
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        DELETE FROM `dbo`.`Orders`
                        WHERE `dbo`.`Orders`.`Id` = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        DELETE FROM "dbo"."Orders"
                        WHERE "dbo"."Orders"."Id" = :0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        DELETE FROM "dbo"."Orders"
                        WHERE "dbo"."Orders"."Id" = $1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        DELETE FROM "dbo"."Orders"
                        WHERE "dbo"."Orders"."Id" = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        DELETE FROM [dbo].[Orders]
                        WHERE [dbo].[Orders].[Id] = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> DeleteMultiTableData
    {
        get
        {
            object?[] expectedParams = [];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        DELETE FROM <<dbo>>.<<Products>>
                        FROM <<Category>> AS <<c1>>
                        WHERE <<dbo>>.<<Products>>.<<CategoryId>> = c1.Id
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        // MySQL accurately extracts the target table to the front of the FROM clause
                        """
                        DELETE `dbo`.`Products`
                        FROM `dbo`.`Products`, `Category` AS `c1`
                        WHERE `dbo`.`Products`.`CategoryId` = c1.Id
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        // Oracle flawlessly transforms the entire AST into an EXISTS subquery block!
                        """
                        DELETE FROM "dbo"."Products"
                        WHERE EXISTS (
                            SELECT 1
                            FROM "Category" "c1"
                            WHERE "dbo"."Products"."CategoryId" = c1.Id
                        )
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        // Postgres perfectly maps the second FROM to USING
                        """
                        DELETE FROM "dbo"."Products"
                        USING "Category" AS "c1"
                        WHERE "dbo"."Products"."CategoryId" = c1.Id
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        DELETE FROM "dbo"."Products"
                        FROM "Category" AS "c1"
                        WHERE "dbo"."Products"."CategoryId" = c1.Id
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        DELETE FROM [dbo].[Products]
                        FROM [Category] AS [c1]
                        WHERE [dbo].[Products].[CategoryId] = c1.Id
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> DeleteTemplateData
    {
        get
        {
            object?[] expectedParams = [TemplateUser.Id];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        DELETE FROM <<Users>>
                        WHERE <<Users>>.<<Id>> = !!100
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        DELETE FROM "Users"
                        WHERE "Users"."Id" = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        DELETE FROM `Users`
                        WHERE `Users`.`Id` = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        DELETE FROM "Users"
                        WHERE "Users"."Id" = :0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        DELETE FROM "Users"
                        WHERE "Users"."Id" = $1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        DELETE FROM "Users"
                        WHERE "Users"."Id" = @p1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        DELETE FROM [Users]
                        WHERE [Users].[Id] = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}