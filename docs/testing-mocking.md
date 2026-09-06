# Testing & Mocking

Because `SqlInterpol` separates query building from database execution, you can unit test your dynamic SQL generation entirely offline without requiring a live database or complex `IDbConnection` mocks.

You can choose between writing standard, single-dialect unit tests or utilizing the advanced specification-driven framework for multi-database compatibility.

---

## Unit Testing

The core testing package (`SqlInterpol.Testing.Xunit`) provides the primitives necessary to validate query structure, parameters, and performance constraints.

### Installation

```bash
dotnet add package SqlInterpol.Testing.Xunit
```

---

### `SqlTestCase` — Snapshot Testing

`SqlTestCase` orchestrates a strict Act → Assert lifecycle. Construct it with the expected outputs, call `Act()` to run the builder, then call `Assert()` to compare.

`SqlTestCase` has two constructors:

```csharp
// Happy-path: expected SQL and optional parameters
new SqlTestCase(string[] expectedSql, object?[]? expectedParameters = null, string? id = null)

// Exception-path: expected throw
new SqlTestCase(Type expectedExceptionType, string? expectedExceptionMessage = null, string? id = null)
```

**Basic SQL assertion:**

```csharp
using SqlInterpol.Testing.Xunit;
using Xunit;

public class OrderQueryTests
{
    [Fact]
    public void Select_ById_GeneratesCorrectSql()
    {
        var testCase = new SqlTestCase(
            expectedSql: [
                """
                SELECT "o"."Id"
                FROM "Orders" AS "o"
                WHERE "o"."order_status" = $1
                """
            ],
            expectedParameters: ["Active"]
        );

        using var db = SqlBuilder.PostgreSql();

        testCase.Act(() =>
        {
            db.Entity<OrderModel>(out var o);
            return db.Append($"""
                SELECT {o.Id}
                FROM {o}
                WHERE {o.Status} = {"Active"}
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }
}
```

**Asserting exceptions** (e.g. unsupported dialect feature):

```csharp
[Fact]
public void Upsert_OnMySql_ThrowsWhenUnsupported()
{
    var testCase = new SqlTestCase(
        expectedExceptionType: typeof(SqlDialectException)
    );

    using var db = SqlBuilder.MySql();

    testCase.Act(() =>
    {
        db.Entity<OrderModel>(out var o);
        return db.Append($"SELECT {o.Id} FROM {o} RETURNING {o.Id}").Build();
    });

    testCase.Assert();
}
```

**Asserting batch queries** (when a single `Act` produces multiple `SqlQueryResult` values):

```csharp
testCase.Act(() =>
{
    // Returns IEnumerable<SqlQueryResult>
    var payloads = new[] { new ProductDto { Id = 1 }, new ProductDto { Id = 2 } };
    return db.AppendInsert(p, payloads).BuildBatch();
});
```

---

### `SqlAssert` — Standalone Assertion Helpers

`SqlAssert` provides the normalized comparison methods used internally by `SqlTestCase`. Use them directly in custom tests that don't follow the Act/Assert lifecycle.

```csharp
// Normalises \r\n → \n before comparing (safe for cross-platform CI)
SqlAssert.MatchesSql(expectedSql, result.Sql);

// Asserts equal length and sequence; treats null and DBNull as identical
SqlAssert.MatchesParameters(expectedParams, result.Parameters.Values.ToArray());
```

---

### `AssertAotIntercepted()` — Pipeline Verification

`db.AssertAotIntercepted()` verifies that the query was routed through the correct compilation pipeline. Its behaviour depends on whether the calling test assembly declares `[SqlInterpolAotEnabledAttribute]`:

| Assembly attribute present? | Assertion |
| :--- | :--- |
| Yes (`AOT_ENABLED` defined) | Fails if the query fell back to the JIT path |
| No (standard JIT project) | Fails if the query was AOT-intercepted unexpectedly |

The attribute is injected automatically by the package's MSBuild targets when you add `AOT_ENABLED` to your project's `DefineConstants`:

```xml
<!-- In your test .csproj -->
<PropertyGroup>
  <DefineConstants>$(DefineConstants);AOT_ENABLED</DefineConstants>
</PropertyGroup>
```

---

### `InspectSegments()` / `InspectScopedVariables()` — Internal State Access

These extension methods expose the builder's internal state under `[InternalsVisibleTo]`, enabling white-box testing of custom preprocessor rules and rewriters without making internals globally public.

```csharp
using var db = SqlBuilder.PostgreSql();
db.Entity<Product>(out var p);
db.Append($"SELECT {p.Id} FROM {p} WHERE {p.Price} > {50m}");

// Inspect the raw token stream before Build()
db.InspectSegments(segments =>
{
    Assert.Contains(segments, s => s.HasTag(SqlSegmentTag.SelectKeyword));
    Assert.Contains(segments, s => s.HasTag(SqlSegmentTag.FromKeyword));
});

// Inspect registered entity variable bindings
db.InspectScopedVariables(vars =>
{
    Assert.True(vars.ContainsKey("p"));
});
```

---

### `MockDialect` — Dialect-Agnostic Structural Tests

`MockDialect` (in `SqlInterpol.Testing.Xunit.Dialects`) is a fully-featured test dialect that supports **all** `SqlFeature` values and uses standard double-quote identifiers with `@p` parameters. Use it when you want to test query structure without asserting dialect-specific quoting or transpilation:

```csharp
using SqlInterpol.Testing.Xunit.Dialects;

using var db = new SqlBuilder(new MockDialect());
db.Entity<Product>(out var p);

var result = db.Append($"""
    SELECT {p.Id} FROM {p}
    ON CONFLICT {p.Id} DO UPDATE SET {p.Name} = {"New"}
    """).Build();

// MockDialect never throws SqlDialectException — safe to assert structure only
Assert.Contains("ON CONFLICT", result.Sql);
```

---

## Specification-Driven Testing

For codebases that support multiple database engines, the `SqlInterpol.Testing.Specifications` package offers a source-generator-backed framework. Instead of rewriting tests for every database, you define a single suite. The Roslyn generator combines your test methods with dialect-specific data to emit fully realized xUnit `[Theory]` classes for each configured dialect.

The package includes a comprehensive set of **pre-built specifications** (like `ISelectTestSuite`, `IWhereTestSuite`, `IUpsertTestSuite`) covering all standard SQL operations. Consume them directly to validate your own custom dialect implementations, or define your own custom specifications for domain-specific queries.

### Installation

```bash
dotnet add package SqlInterpol.Testing.Specifications
```

---

### Key Attributes

| Attribute | Target | Purpose |
| :--- | :--- | :--- |
| `[SqlTestSuite(typeof(IMySpec))]` | Abstract partial class | Links the suite template to its specification interface |
| `[SqlTest("DataPropertyName")]` | Method | Marks the method as the implementation target; generator replaces it with `[Theory][MemberData]` |
| `[SqlGeneratorIgnore]` | Any member | Excludes the member from generator output (required on abstract `CreateBuilder`) |

---

### 1. Defining the Contract (Specification)

Create an interface inheriting from `ISqlTestSuiteBase`. Define `static abstract TheoryData<SqlTestCase>` properties for each test scenario.

```csharp
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace MyApp.Tests.Specifications;

public interface ISelectAsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> ProjectionAsLiteralData { get; }
    static abstract TheoryData<SqlTestCase> RawColumnAsProjectionData { get; }
}
```

---

### 2. Implementing the Test Logic

Decorate an abstract partial class with `[SqlTestSuite]` and write tests with `[SqlTest]`. The source generator wires up `[Theory]` and `[MemberData]` in the emitted partial.

```csharp
using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
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

---

### 3. Providing the Expected Test Data

Implement the interface for each dialect as a `partial` class. Supply the exact expected SQL and parameters for that dialect. The source generator merges the generated execution code directly into this partial class.

```csharp
using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace MyApp.Tests.Dialects.PostgreSql;

public partial class PostgreSqlSelectAsTestSuite : ISelectAsTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null)
        => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> ProjectionAsLiteralData => [
        new SqlTestCase(
            expectedSql: [
                """
                SELECT
                    "Products"."Id" AS ProductId
                FROM "Products"
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> RawColumnAsProjectionData => [
        new SqlTestCase(
            expectedSql: ["""SELECT "Products"."Id" FROM "Products\""""]
        )
    ];
}
```

---

### 4. The Generated Execution Class (Behind the Scenes)

The source generator emits the other half of the `partial` class, wiring the abstract test methods to the `[Theory]` data and providing the standard `Product` fixture model. Clickable source links in the generated output navigate back to the original test data file.

```csharp
// <auto-generated/>
namespace MyApp.Tests.Dialects.PostgreSql;

public partial class PostgreSqlSelectAsTestSuite
{
    [SqlTable("Products")]
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // Ctrl+Click → MyApp.Tests.Dialects.PostgreSql.PostgreSqlSelectAsTestSuite.cs:12
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