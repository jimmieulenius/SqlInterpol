using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class FromTests
{
    // Local models to test the specific attribute configurations
    [SqlTable("MyTable")]
    public record TableOnlyModel(int Id);

    [SqlTable("MyTable", Schema = "MySchema")]
    public record TableAndSchemaModel(int Id);

    [Theory]
    [MemberData(nameof(FromSingleEntityData))]
    public void From_SingleEntity(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<OrderLine>(ol =>
            db.Append($$"""
            SELECT *
            FROM {{ol}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> FromSingleEntityData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            """
            SELECT *
            FROM <<OrderLine>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            """
            SELECT *
            FROM `OrderLine`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            """
            SELECT *
            FROM "OrderLine"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            """
            SELECT *
            FROM "OrderLine"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT *
            FROM "OrderLine"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            """
            SELECT *
            FROM [OrderLine]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(FromTableNameOnlyData))]
    public void From_Entity_WithSqlTableNameOnly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<TableOnlyModel>(m =>
            db.Append($$"""
            SELECT *
            FROM {{m}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> FromTableNameOnlyData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            """
            SELECT *
            FROM <<MyTable>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            """
            SELECT *
            FROM `MyTable`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            """
            SELECT *
            FROM "MyTable"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            """
            SELECT *
            FROM "MyTable"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT *
            FROM "MyTable"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            """
            SELECT *
            FROM [MyTable]
            """
        )
    ];

    [Theory]
    [MemberData(nameof(FromTableNameAndSchemaData))]
    public void From_Entity_WithSqlTableNameAndSchema(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Query<TableAndSchemaModel>(m =>
            db.Append($$"""
            SELECT *
            FROM {{m}}
            """));
        
        // Act
        var result = db.Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql, result.Sql);
    }

    public static TheoryData<SqlTestCase> FromTableNameAndSchemaData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            """
            SELECT *
            FROM <<MySchema>>.<<MyTable>>
            """
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            """
            SELECT *
            FROM `MySchema`.`MyTable`
            """
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            """
            SELECT *
            FROM "MySchema"."MyTable"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            """
            SELECT *
            FROM "MySchema"."MyTable"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            """
            SELECT *
            FROM "MySchema"."MyTable"
            """
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            """
            SELECT *
            FROM [MySchema].[MyTable]
            """
        )
    ];
}