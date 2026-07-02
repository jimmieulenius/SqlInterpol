using System;
using System.Linq;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class UpdateTests
{
    // Shared test data at the class level ensures zero drift between execution and assertions!
    private const int TargetOrderId = 42;
    private const string TargetStatus = "Shipped";
    private const decimal TargetTotal = 99.99m;
    private static readonly TestUser TemplateUser = new() { Id = 1, Name = "Bob", Age = 31 };

    [Theory]
    [MemberData(nameof(UpdateData))]
    public void Update_WithContextualDto(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { Status = TargetStatus, Total = TargetTotal };

        // Act
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$"""
                UPDATE {{o}}
                SET {{updateDto}}
                WHERE {{o.Id}} = {{TargetOrderId}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(UpdateExplicitData))]
    public void Update_PureManual(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$"""
                UPDATE {{o}}
                SET {{o.Status}} = {{TargetStatus}}, {{o.Total}} = {{TargetTotal}}
                WHERE {{o.Id}} = {{TargetOrderId}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(UpdateWithIgnoreData))]
    public void Update_WithIgnoredProperty(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var order = new OrderWithIgnoreModel 
        { 
            Id = TargetOrderId, 
            Status = TargetStatus, 
            Total = TargetTotal, 
            InternalNotes = "Ignore me!" 
        };

        // Act
        testCase.Action(() => db.Entity<OrderWithIgnoreModel>(out var o)
            .Append($$"""
                UPDATE {{o}}
                SET {{order}}
                WHERE {{o.Id}} = {{order.Id}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(UpdateInvalidEntityData))]
    public void Update_InvalidEntity(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() =>
        {
            Sql.BuildAssignments(new InvalidDummyEntity(), new { Name = "Test" }, db.Context);

            return new SqlQueryResult(string.Empty, new Dictionary<string, object?>());
        });

        // Assert
        testCase.Assert();
    }

    // [Theory]
    // [MemberData(nameof(UpdateInvalidEntityPropertyData))]
    // public void Update_InvalidEntityProperty(SqlTestCase testCase)
    // {
    //     // Arrange
    //     var db = testCase.CreateBuilder();

    //     // Act
    //     testCase.Action(() =>
    //     {
    //         var entity = db.AddEntity<Product>();
    //         Sql.BuildAssignments(entity, new { Id = 1, NonExistentProperty = "Should Fail" }, db.Context);

    //         return new SqlQueryResult(string.Empty, new Dictionary<string, object?>());
    //     });

    //     // Assert
    //     testCase.Assert();
    // }

    [Theory]
    [MemberData(nameof(MultiTableUpdateData))]
    public void Update_MultiTable(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Implicit Join WYSIWYG!
        testCase.Action(() => db
            .Entity<Product>(out var p)
            .Entity<Category>(out var c)
            .Append($$"""
                UPDATE {{p}}
                SET {{p.Price}} = {{10}}
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
    // [MemberData(nameof(UpdateTemplateData))]
    // public void AppendUpdate_Template(SqlTestCase testCase)
    // {
    //     // Arrange
    //     var db = testCase.CreateBuilder();
    //     var user = new TestUser { Id = 1, Name = "Bob", Age = 31 };

    //     // Act
    //     testCase.Action(() => db.Query<TestUser>(u => 
    //          db.AppendUpdate(u, user, x => x.Id)
    //     ).Build());

    //     // Assert
    //     testCase.Assert();
    // }

    public static TheoryData<SqlTestCase> UpdateData
    {
        get
        {
            object?[] expectedParams = [TargetStatus, TargetTotal, TargetOrderId];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        UPDATE <<dbo>>.<<Orders>>
                        SET <<order_status>> = !!100, <<Total>> = !!101
                        WHERE <<dbo>>.<<Orders>>.<<Id>> = !!102
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        UPDATE `dbo`.`Orders`
                        SET `order_status` = @p0, `Total` = @p1
                        WHERE `dbo`.`Orders`.`Id` = @p2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        UPDATE "dbo"."Orders"
                        SET "order_status" = :0, "Total" = :1
                        WHERE "dbo"."Orders"."Id" = :2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        UPDATE "dbo"."Orders"
                        SET "order_status" = $1, "Total" = $2
                        WHERE "dbo"."Orders"."Id" = $3
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        UPDATE "dbo"."Orders"
                        SET "order_status" = @p1, "Total" = @p2
                        WHERE "dbo"."Orders"."Id" = @p3
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        UPDATE [dbo].[Orders]
                        SET [order_status] = @p0, [Total] = @p1
                        WHERE [dbo].[Orders].[Id] = @p2
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> UpdateExplicitData
    {
        get
        {
            object?[] expectedParams = [TargetStatus, TargetTotal, TargetOrderId];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        UPDATE <<dbo>>.<<Orders>>
                        SET <<dbo>>.<<Orders>>.<<order_status>> = !!100, <<dbo>>.<<Orders>>.<<Total>> = !!101
                        WHERE <<dbo>>.<<Orders>>.<<Id>> = !!102
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        UPDATE `dbo`.`Orders`
                        SET `dbo`.`Orders`.`order_status` = @p0, `dbo`.`Orders`.`Total` = @p1
                        WHERE `dbo`.`Orders`.`Id` = @p2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        UPDATE "dbo"."Orders"
                        SET "dbo"."Orders"."order_status" = :0, "dbo"."Orders"."Total" = :1
                        WHERE "dbo"."Orders"."Id" = :2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        UPDATE "dbo"."Orders"
                        SET "dbo"."Orders"."order_status" = $1, "dbo"."Orders"."Total" = $2
                        WHERE "dbo"."Orders"."Id" = $3
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        UPDATE "dbo"."Orders"
                        SET "dbo"."Orders"."order_status" = @p1, "dbo"."Orders"."Total" = @p2
                        WHERE "dbo"."Orders"."Id" = @p3
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        UPDATE [dbo].[Orders]
                        SET [dbo].[Orders].[order_status] = @p0, [dbo].[Orders].[Total] = @p1
                        WHERE [dbo].[Orders].[Id] = @p2
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> UpdateWithIgnoreData
    {
        get
        {
            object?[] expectedParams = [TargetOrderId, TargetStatus, TargetTotal, TargetOrderId];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        UPDATE <<dbo>>.<<Orders>>
                        SET <<Id>> = !!100, <<order_status>> = !!101, <<Total>> = !!102
                        WHERE <<dbo>>.<<Orders>>.<<Id>> = !!103
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        UPDATE `dbo`.`Orders`
                        SET `Id` = @p0, `order_status` = @p1, `Total` = @p2
                        WHERE `dbo`.`Orders`.`Id` = @p3
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        UPDATE "dbo"."Orders"
                        SET "Id" = :0, "order_status" = :1, "Total" = :2
                        WHERE "dbo"."Orders"."Id" = :3
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        UPDATE "dbo"."Orders"
                        SET "Id" = $1, "order_status" = $2, "Total" = $3
                        WHERE "dbo"."Orders"."Id" = $4
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        UPDATE "dbo"."Orders"
                        SET "Id" = @p1, "order_status" = @p2, "Total" = @p3
                        WHERE "dbo"."Orders"."Id" = @p4
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        UPDATE [dbo].[Orders]
                        SET [Id] = @p0, [order_status] = @p1, [Total] = @p2
                        WHERE [dbo].[Orders].[Id] = @p3
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> UpdateInvalidEntityData
    {
        get
        {
            var expectedExceptionType = typeof(ArgumentException);
            var expectedExceptionMessage = "Entity must implement ISqlEntityBase<T>";

            return [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> UpdateInvalidEntityPropertyData
    {
        get
        {
            var expectedExceptionType = typeof(ArgumentException);
            var expectedExceptionMessage = "Property 'NonExistentProperty' on DTO does not exist on Entity.";

            return [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    expectedExceptionType: expectedExceptionType,
                    expectedExceptionMessage: expectedExceptionMessage
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> MultiTableUpdateData
    {
        get
        {
            object?[] expectedParams = [10];

            return
            [
                new SqlTestCase(SqlDialectKind.CustomDb, typeof(SqlDialectException)),
                new SqlTestCase(SqlDialectKind.Firebird, typeof(SqlDialectException)),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        UPDATE `dbo`.`Products`, `Category` AS `c1`
                        SET `Price` = @p0
                        WHERE `dbo`.`Products`.`CategoryId` = c1.Id
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        MERGE INTO "dbo"."Products"
                        USING "Category" "c1"
                        ON ("dbo"."Products"."CategoryId" = c1.Id)
                        WHEN MATCHED THEN UPDATE SET "Price" = :0
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        UPDATE "dbo"."Products"
                        SET "Price" = $1
                        FROM "Category" AS "c1"
                        WHERE "dbo"."Products"."CategoryId" = c1.Id
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        UPDATE "dbo"."Products"
                        SET "Price" = @p1
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
                        UPDATE [dbo].[Products]
                        SET [Price] = @p0
                        FROM [Category] AS [c1]
                        WHERE [dbo].[Products].[CategoryId] = c1.Id
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> UpdateTemplateData
    {
        get
        {
            object?[] expectedParams = [TemplateUser.Age, TemplateUser.Name, TemplateUser.Id];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        UPDATE <<Users>>
                        SET <<Age>> = !!100, <<Name>> = !!101
                        WHERE <<Users>>.<<Id>> = !!102
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        UPDATE "Users"
                        SET "Age" = @p0, "Name" = @p1
                        WHERE "Users"."Id" = @p2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        UPDATE `Users`
                        SET `Age` = @p0, `Name` = @p1
                        WHERE `Users`.`Id` = @p2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        UPDATE "Users"
                        SET "Age" = :0, "Name" = :1
                        WHERE "Users"."Id" = :2
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        UPDATE "Users"
                        SET "Age" = $1, "Name" = $2
                        WHERE "Users"."Id" = $3
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        UPDATE "Users"
                        SET "Age" = @p1, "Name" = @p2
                        WHERE "Users"."Id" = @p3
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        UPDATE [Users]
                        SET [Age] = @p0, [Name] = @p1
                        WHERE [Users].[Id] = @p2
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}