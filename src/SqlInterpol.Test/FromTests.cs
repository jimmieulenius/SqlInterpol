using SqlInterpol.Test.Models;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Configuration;

namespace SqlInterpol.Test;

public class FromTests
{
    [Theory]
    [MemberData(nameof(From_SingleEntityData))]
    public void From_SingleEntity(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() =>
        {
            db.Entity<OrderLine>(out var ol);
            return db.Append($$"""
                SELECT *
                FROM {{ol}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [Theory]
    [MemberData(nameof(From_EntityWithSqlTableNameOnlyData))]
    public void From_Entity_WithSqlTableNameOnly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() =>
        {
            db.Entity<TableOnlyModel>(out var m);
            return db.Append($$"""
                SELECT *
                FROM {{m}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [Theory]
    [MemberData(nameof(From_Entity_WithSqlTableNameAndSchemaData))]
    public void From_Entity_WithSqlTableNameAndSchema(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() =>
        {
            db.Entity<TableAndSchemaModel>(out var m);
            return db.Append($$"""
                SELECT *
                FROM {{m}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    public static TheoryData<SqlTestCase> From_SingleEntityData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<OrderLine>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT *
                FROM "OrderLine"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `OrderLine`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "OrderLine"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "OrderLine"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "OrderLine"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [OrderLine]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> From_EntityWithSqlTableNameOnlyData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<MyTable>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT *
                FROM "MyTable"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `MyTable`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "MyTable"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "MyTable"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "MyTable"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [MyTable]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> From_Entity_WithSqlTableNameAndSchemaData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<MySchema>>.<<MyTable>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT *
                FROM "MySchema"."MyTable"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `MySchema`.`MyTable`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "MySchema"."MyTable"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "MySchema"."MyTable"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "MySchema"."MyTable"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [MySchema].[MyTable]
                """
            ]
        )
    ];
}