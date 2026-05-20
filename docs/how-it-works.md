# How It Works

This page explains the mechanics behind SqlInterpol's key features.

## Name Mapping — Attributes and Runtime Overrides

By default SqlInterpol uses the C# type name as the table name and property names as column names. Use `[SqlTable]` and `[SqlColumn]` to override them at the model level:

```csharp
[SqlTable("products", Schema = "inventory")]
public class Product
{
    public int Id { get; set; }

    [SqlColumn("prod_name")]
    public string Name { get; set; } = null!;

    public decimal Price { get; set; }
}
```

**Generated SQL (PostgreSQL):**
```sql
INSERT INTO "inventory"."products" ("Id", "prod_name", "Price")
VALUES ($1, $2, $3)
```

You can also override name and schema at query time without touching the class — useful when querying a different table in the same shape (e.g. an archive table):

```csharp
var db = SqlBuilder.PostgreSql();

var query = db.Entity<Product>(name: "products_archive", schema: "archive")
    .Query(p => db.Append($$"""
        SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
        FROM {{p}}
        WHERE {{p[x => x.Price]}} < {{100m}}
    """)).Build();
```

**Generated SQL (PostgreSQL):**
```sql
SELECT "products_archive"."Id", "products_archive"."prod_name"
FROM "archive"."products_archive"
WHERE "products_archive"."Price" < $1
```

## Safe IN Clauses (Collection Expansion)

No more string-joining arrays. Pass any `IEnumerable<T>` directly into the string and SqlInterpol safely expands it into numbered parameters:

```csharp
var ids = new List<int> { 1, 2, 3 };
db.Append($"SELECT {p[x => x.Id]}, {p[x => x.Name]} FROM {p} WHERE {p[x => x.Id]} IN ({ids})");
```

**Generated SQL (PostgreSQL):**
```sql
SELECT "Product"."Id", "Product"."Name"
FROM "Product"
WHERE "Product"."Id" IN ($1, $2, $3)
```

Collection expansion scales sub-linearly — going from 5 to 100 items in an `IN` clause adds only ~6.7 μs (see [Benchmarks](benchmarks.md)).

## Dynamic SQL without the Danger

SqlInterpol strictly enforces parameterization. The Roslyn Analyzer will prevent you from passing raw variables into `Append()`. SQL keywords and identifiers cannot be parameterized, so when you genuinely need a dynamic identifier, use `Sql.Raw()` — but always validate against an allowlist first:

```csharp
// User picks an aggregate function from a report UI
string[] allowed = ["SUM", "AVG", "MAX", "MIN"];
if (!allowed.Contains(request.Aggregate)) throw new ArgumentException("Invalid aggregate");

db.Append($"SELECT {Sql.Raw(request.Aggregate)}(Amount) FROM Orders WHERE CustomerId = {customerId}");
// SQL keywords can't be parameters — Sql.Raw() is the explicit, auditable escape hatch.
// "Amount" and table name stay quoted identifiers; only the function name is injected raw.
```

Every `Sql.Raw()` call is a visible, searchable audit point — code review can grep for it and reviewers know exactly where raw injection occurs.

## Alias Resolution

Declare a table alias once and every column expression in that query resolves to it automatically:

```csharp
var query = db.Query<Product, Category>((p, c) => db.Append($$"""
    SELECT
        {{p[x => x.Id]}},
        {{p[x => x.Name]}},
        {{c[x => x.Name]}} AS CategoryName
    FROM {{p}} AS prod
    JOIN {{c}} AS cat
        ON {{p[x => x.CategoryId]}} = {{c[x => x.Id]}}
""")).Build();
```

**Generated SQL (PostgreSQL):**
```sql
SELECT
    prod."Id",
    prod."Name",
    cat."Name" AS CategoryName
FROM "Product" AS prod
JOIN "Category" AS cat
    ON prod."CategoryId" = cat."Id"
```

The alias is declared once on `FROM {{p}} AS prod` — all subsequent `{{p[x => ...]}}` references resolve to `prod."ColumnName"` automatically. Rename the alias in one place and it propagates everywhere.

## Composable Subqueries

An `ISqlQuery<T>` returned by `.Query(...)` can be interpolated directly into any outer query — as a `FROM` subquery, a `WHERE ... IN (...)` filter, or a correlated `SELECT` expression. The outer query treats it like any other typed entity, so you can reference its projected columns with the same expression syntax.

**WHERE IN subquery:**

```csharp
var db = SqlBuilder.PostgreSql();

// Step 1: build the inner query as a variable
var activeCategoryIds = db.Entity<Product>(alias: "p")
    .Query(p => db.Append($$"""
        SELECT {{p[x => x.CategoryId]}}
        FROM {{p}}
        WHERE {{p[x => x.Price]}} > {{0m}}
        """));

// Step 2: interpolate it directly into the outer query
var result = db.Entity<Category>(alias: "c")
    .Query(c => db.Append($$"""
        SELECT {{c[x => x.Name]}}
        FROM {{c}}
        WHERE {{c[x => x.Id]}} IN
        (
            {{activeCategoryIds}}
        )
        """))
    .Build();
```

**Generated SQL (PostgreSQL):**
```sql
SELECT c."Name"
FROM "Category" AS c
WHERE c."Id" IN
(
    SELECT p."CategoryId"
    FROM "Product" AS p
    WHERE p."Price" > $1
)
```

**FROM subquery with typed column access:**

```csharp
var stats = db.Entity<CategoryStats>()
    .Entity<Product>(alias: "p")
    .Query((s, p) => db.Append($$"""
        SELECT
            {{p[x => x.CategoryId]}} AS {{s[x => x.CategoryId]}},
            SUM({{p[x => x.Price]}}) AS {{s[x => x.TotalPrice]}}
        FROM {{p}}
        GROUP BY {{p[x => x.CategoryId]}}
        """));

var result = db.Entity<Category>(alias: "c")
    .Query(c => db.Append($$"""
        SELECT {{c[x => x.Name]}}, {{stats[x => x.TotalPrice]}}
        FROM
        (
            {{stats}}
        ) AS {{"stats"}}
        JOIN {{c}} ON {{stats[x => x.CategoryId]}} = {{c[x => x.Id]}}
        """))
    .Build();
```

**Generated SQL (PostgreSQL):**
```sql
SELECT c."Name", stats."TotalPrice"
FROM
(
    SELECT p."CategoryId" AS "CategoryId", SUM(p."Price") AS "TotalPrice"
    FROM "Product" AS p
    GROUP BY p."CategoryId"
) AS stats
JOIN "Category" AS c ON stats."CategoryId" = c."Id"
```

The subquery variable carries its typed projection — `{{stats[x => x.TotalPrice]}}` in the outer query resolves to `stats."TotalPrice"` automatically, with full compile-time safety.

## Dependency Injection Setup

For ASP.NET Core or any `IServiceCollection`-based host, register SqlInterpol once at startup:

```csharp
// Program.cs
builder.Services.AddSqlInterpol();

// Or with custom options (formatting, parser overrides, etc.):
builder.Services.AddSqlInterpol(new SqlInterpolOptions
{
    CollectionLayout = SqlCollectionLayout.Vertical,
    IndentSize = 4,
});
```

`AddSqlInterpol()` registers `SqlInterpolOptions` and `ISqlInterpolationParser` as singletons. The builder can then be constructed from any `IDbConnection` or `DbContext` as normal — the registered options are picked up automatically.
