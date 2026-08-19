using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IFromTestSuite))]
public abstract partial class FromTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IFromTestSuite.From_SingleEntityData))]
    public void From_SingleEntity(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
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

    [SqlTest(nameof(IFromTestSuite.From_EntityWithSqlTableNameOnlyData))]
    public void From_Entity_WithSqlTableNameOnly(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
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

    [SqlTest(nameof(IFromTestSuite.From_Entity_WithSqlTableNameAndSchemaData))]
    public void From_Entity_WithSqlTableNameAndSchema(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
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
}