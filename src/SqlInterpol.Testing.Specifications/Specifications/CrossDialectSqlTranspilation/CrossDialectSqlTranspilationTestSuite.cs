using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ICrossDialectSqlTranspilationTestSuite))]
public abstract partial class CrossDialectSqlTranspilationTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder();

    [SqlTest(nameof(ICrossDialectSqlTranspilationTestSuite.PagingToggleData))]
    public void Paging_Toggle(bool crossDialectSqlTranspilation, SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.CrossDialectSqlTranspilation = crossDialectSqlTranspilation;
        int limit = 10;
        int offset = 20;
        
        // Act
        testCase.Act(() => 
        {
            return [db.Append($"SELECT * FROM Products LIMIT {limit} OFFSET {offset}").Build()];
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(ICrossDialectSqlTranspilationTestSuite.PagingHardcodedToggleData))]
    public void Paging_Hardcoded_Toggle(bool crossDialectSqlTranspilation, SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.CrossDialectSqlTranspilation = crossDialectSqlTranspilation;

        // Act
        testCase.Act(() => 
        {
            return [db.Append($"SELECT * FROM Products LIMIT 10 OFFSET 20").Build()];
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(ICrossDialectSqlTranspilationTestSuite.RowLockingToggleData))]
    public void RowLocking_Toggle(bool crossDialectSqlTranspilation, SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.CrossDialectSqlTranspilation = crossDialectSqlTranspilation;

        // Act
        testCase.Act(() => 
        {
            return [db.Append($"SELECT * FROM Products FOR UPDATE").Build()];
        });

        // Assert
        testCase.Assert();
    }

    [SqlTest(nameof(ICrossDialectSqlTranspilationTestSuite.SelectIntoToggleData))]
    public void SelectInto_Toggle(bool crossDialectSqlTranspilation, SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.CrossDialectSqlTranspilation = crossDialectSqlTranspilation;

        // Act
        testCase.Act(() => 
        {
            return [db.Append($"SELECT Id INTO #Temp FROM Products").Build()];
        });

        // Assert
        testCase.Assert();
    }
}