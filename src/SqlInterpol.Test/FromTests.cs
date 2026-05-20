using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

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
        var result = db.Query<OrderLine>(ol =>
            db.Append($$"""
            SELECT *
            FROM {{ol}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(From_EntityWithSqlTableNameOnlyData))]
    public void From_Entity_WithSqlTableNameOnly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<TableOnlyModel>(m =>
            db.Append($$"""
            SELECT *
            FROM {{m}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(From_Entity_WithSqlTableNameAndSchemaData))]
    public void From_Entity_WithSqlTableNameAndSchema(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<TableAndSchemaModel>(m =>
            db.Append($$"""
            SELECT *
            FROM {{m}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
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
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `MySchema`.`MyTable`
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