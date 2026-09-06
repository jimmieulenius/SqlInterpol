# Testing & Mocking

Because `SqlInterpol` separates query building from database execution, you can unit test your dynamic SQL generation entirely offline without requiring a live database or complex `IDbConnection` mocks. 

You can choose between writing standard, single-dialect unit tests or utilizing the advanced specification-driven framework for multi-database compatibility.

---

## Unit Testing

The core testing package (`SqlInterpol.Testing.Xunit`) provides the primitives necessary to validate your query structures and ensure performance constraints are met. 

### Using `SqlTestCase`

For snapshot-based and data-driven testing, the package provides the `SqlTestCase` object to orchestrate a strict validation lifecycle using xUnit. It natively compares your dynamic `SqlQueryResult` against expected baselines.

*   **`testCase.Act(...)`**: Wraps your `SqlBuilder` execution. It captures the rendered SQL string, parameterized variables, and any thrown exceptions.
*   **`testCase.Assert()`**: Compares the captured result against the expected baseline. If the SQL string, parameters, or expected exceptions deviate, the test fails.
*   **`db.AssertAotIntercepted()`**: Verifies the C# compiler successfully routed the interpolated string through the Roslyn interceptors, preventing hidden JIT-fallback performance regressions.

```csharp
using System.Collections.Generic;
using SqlInterpol.Testing.Xunit;
using Xunit;

public class OrderQueryTests
{
    [Fact]
    public void Select_ActiveOrders_GeneratesCorrectSql()
    {
        var testCase = new SqlTestCase
        {
            Sql = "SELECT [o].[Id] FROM [Orders] AS [o] WHERE [o].[order_status] = @p0",
            Parameters = new Dictionary<string, object?> { { "@p0", "Active" } }
        };

        using var db = new SqlBuilder(); 
        
        testCase.Act(() => 
        {
            db.Entity<OrderModel>(out var o);
            return db.Append($"""
                SELECT {o.Id} FROM {o} WHERE {o.Status} = {"Active"}
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }
}
```

### Handling Exceptions

To assert that invalid builder configurations or malformed schema mappings throw the correct exceptions, define the expected type and message directly on the test case:

```csharp
[Fact]
public void Select_InvalidColumn_ThrowsArgumentException()
{
    var testCase = new SqlTestCase
    {
        ExpectedExceptionType = typeof(ArgumentException),
        ExpectedExceptionMessage = "FakeColumn" 
    };

    using var db = new SqlBuilder(); 
    
    testCase.Act(() => 
    {
        db.Entity<OrderModel>(out var o);
        return db.Append($"SELECT {o.Column("FakeColumn")} FROM {o}").Build();
    });

    testCase.Assert();
}
```

---

## Specification-Driven Testing

For codebases that support multiple database engines, the `SqlInterpol.Testing.Specifications` package offers a source-generator-backed framework. Instead of rewriting tests for every database, you define a single suite. The Roslyn generator combines your test methods with dialect-specific data to emit fully realized xUnit `[Theory]` classes for each configured dialect.

The package includes a comprehensive set of **pre-built specifications** (like `ISelectTestSuite`, `IWhereTestSuite`, `IUpsertTestSuite`) covering all standard SQL operations. You can consume these directly to validate your own custom dialect implementations, or define your own custom specifications for domain-specific queries.

### 1. Defining the Contract (Specification)

Create an interface inheriting from `ISqlTestSuiteBase`. Define xUnit `TheoryData<SqlTestCase>` properties to represent your required test scenarios.

```csharp
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace MyApp.Tests.Specifications;

public interface ISelectAsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> ProjectionAsLiteralData { get; }
    static abstract TheoryData<SqlTestCase> RawColumnAsProjectionData { get; }
}
```

### 2. Implementing the Test Logic

Decorate an abstract partial class with `[SqlTestSuite]` and write your test using `[SqlTest]`. The source generator automatically pairs these `[SqlTest]` methods with the `TheoryData` implementation (defined in the next step).

```csharp
using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace MyApp.Tests.Specifications;

[SqlTestSuite(typeof(ISelectAsTestSuite))]
public abstract partial class SelectAsTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(ISelectAsTestSuite.ProjectionAsLiteralData))]
    public void SelectAs_LiteralProjection(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Id}} AS ProductId
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }
}
```

### 3. Providing the Expected Test Data

Implement the interface for each dialect you support using a **`partial` class** to supply the exact expected SQL transpilation and parameters. The Roslyn source generator merges its generated execution code directly into this partial class.

```csharp
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace MyApp.Tests.Dialects.SqlServer;

public partial class SqlServerSelectAsTestSuite : ISelectAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null)
        => new SqlBuilder(new SqlServerDialect(), options);

    public static TheoryData<SqlTestCase> ProjectionAsLiteralData => new()
    {
        new SqlTestCase
        {
            Sql = "SELECT [p].[Id] AS ProductId FROM [Products] AS [p]"
        }
    };
    
    // ... Implement other required TheoryData properties
}
```

### 4. The Generated Execution Class (Behind the Scenes)

During compilation, the Roslyn source generator merges your test logic and dialect data using the `partial` class structure. It automatically emits the other half of your dialect class, creating fully functional xUnit test methods complete with clickable file links back to your original source code.

```csharp
// <auto-generated/>
#nullable enable
using SqlInterpol;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;
using SqlInterpol.Configuration;

namespace MyApp.Tests.Dialects.SqlServer;

// The generator merges this partial class with your test data class
public partial class SqlServerSelectAsTestSuite
{
    [SqlTable("Products", "dbo")]
    public class Product
    {
        public int Id { get; set; }
        
        [SqlColumn("PROD_NAME")]
        public string Name { get; set; } = "";
    }

    // Ctrl+Click to edit test data: file:///Local:/Path/To/SqlServerSelectAsTestSuite.cs#12
    [Theory]
    [MemberData(nameof(ProjectionAsLiteralData))]
    public void SelectAs_LiteralProjection(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Id}} AS ProductId
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }
}
```