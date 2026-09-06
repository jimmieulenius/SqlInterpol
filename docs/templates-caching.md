# Query Templates & Performance

For ultra-high-throughput hot paths, re-parsing interpolated strings on every request introduces parsing and allocation overhead. `SqlInterpol` allows you to pre-compile query structures into reusable `ISqlTemplate` instances, executing them repeatedly with zero stream-processing overhead and $O(1)$ parameter binding.

---

## Roslyn Analyzer Enforcement (`SQLIA07`)

To guarantee maximum runtime performance, `SqlInterpol` includes a Roslyn analyzer (`SQLIA07`) that detects when `.Template()` is invoked on a dynamic execution path.

> ⚠️ **SQLIA07: Template initialized on execution path**  
> *Templates initialized on the execution path allocate memory. Move this Template to a static readonly field, or use standard `db.Append()` to utilize the AOT compiler.*

### Incorrect Usage (Triggers `SQLIA07`)

Calling `.Template()` inside an instance method or request handler compiles the template on every invocation, defeating the purpose of pre-compilation:

```csharp
public void ProcessOrder(SqlBuilder db, int orderId)
{
    db.Entity<OrderModel>(out var o);

    // WARNING SQLIA07: Called inside an instance method execution path
    db.Template(out var template, $$"""
        SELECT {o.Id} FROM {o} WHERE {o.Id} = {Sql.Arg("Id")}
        """);

    db.Append(template, new { Id = orderId });
}
```

---

## Defining Static Templates

In standard application code where the database dialect is fixed, store compiled templates directly in `static readonly` fields. This completely eliminates dictionary lookups, lock contention, and allocation overhead.

### Correct Usage (Satisfies `SQLIA07`)

Initialize static templates during application startup or within a static constructor:

```csharp
public static class OrderQueries
{
    // Satisfies SQLIA07: Static readonly template compiled once at class load
    public static readonly ISqlTemplate ActiveOrders = CompileActiveOrdersTemplate();

    private static ISqlTemplate CompileActiveOrdersTemplate()
    {
        using var db = new SqlBuilder(); // Configured with application default dialect
        db.Entity<OrderModel>(out var o);

        db.Template(out var template, $$"""
            SELECT {o.Id}, {o.CustomerId}
            FROM {o} AS o1
            WHERE {o.CustomerId} = {Sql.Arg("CustId")}
            """);

        return template;
    }
}
```

> ℹ️ **Multi-Dialect Environments & Testing**  
> If your codebase dynamically switches dialects at runtime (such as cross-dialect unit test suites), you can alternatively cache static templates in a `ConcurrentDictionary<SqlDialectKind, ISqlTemplate>`. For typical single-database applications, a simple `static readonly` field is the idiomatic standard.

---

## Executing Templates

Pass the compiled `ISqlTemplate` to `db.Append()` along with an anonymous object or DTO. Additional clauses (such as dynamic sorting or pagination) can be appended after the template.

```csharp
public async Task<IEnumerable<OrderModel>> GetOrdersAsync(IDbConnection conn, int customerId)
{
    using var db = conn.CreateSqlBuilder();
    db.Entity<OrderModel>(out var o);

    // Injects parameters without stream parsing or memory allocations
    var query = db.Append(OrderQueries.ActiveOrders, new { CustId = customerId })
                  .AppendLine()
                  .Append($"ORDER BY {o.Id} DESC")
                  .Build();

    return await conn.QueryAsync<OrderModel>(query);
}
```

---

## Template Macros

To build flexible, reusable templates, `SqlInterpol` provides specialized macros that define parameter placeholders and dynamically expand object schemas during compilation.

### 1. Named Parameters (`Sql.Arg`)

The `Sql.Arg("PropertyName")` macro defines an explicit parameter placeholder inside a template. During execution, `SqlInterpol` matches this string name against the properties of the payload object passed to `db.Append()`.

```csharp
db.Template(out var template, $$"""
    SELECT {o.Id} 
    FROM {o} 
    WHERE {o.CustomerId} = {Sql.Arg("CustId")}
    """);

// "CustId" matches the anonymous object property
db.Append(template, new { CustId = 5 });
```

### 2. Auto-Expansion (`Sql.Expand<T>`)

When creating templates for DML operations, use `Sql.Expand<T>()` to automatically generate full column lists and parameter placeholders for an entire POCO or DTO.

**Manual `INSERT` Expansion:**  
Expands to `(Col1, Col2) VALUES ({0}, {1})` and binds parameters automatically:

```csharp
db.Entity<OrderModel>(out var o);

db.Template(out var insertTemplate, $$"""
    INSERT INTO {o}
    VALUES {Sql.Expand<OrderInsertPayload>()}
    """);

db.Append(insertTemplate, new OrderInsertPayload { Id = 101, CustomerId = 5 });
```

**Manual `UPDATE` Expansion:**  
Pass property names to `Sql.Expand<T>("ExcludedProperty")` to omit them from the generated `SET` list. This is essential for excluding primary keys from the update list so they can be placed safely in the `WHERE` clause:

```csharp
db.Entity<OrderModel>(out var o);

db.Template(out var updateTemplate, $$"""
    UPDATE {o}
    SET {Sql.Expand<OrderUpdatePayload>("Id")}
    WHERE {o.Id:col} = {Sql.Arg("Id")}
    """);

db.Append(updateTemplate, new OrderUpdatePayload { Id = 101, CustomerId = 500 });
```

---

## Built-In Bulk Operations

For bulk array operations, manual template construction is unnecessary. `SqlInterpol` provides built-in extensions that manage dialect-specific template compilation and caching under the hood:

* **Bulk Insert:** `db.AppendInsert(entity, payloads)`
* **Bulk Update:** `db.AppendUpdate(entity, entity.Id, payloads)`
* **Bulk Upsert:** `db.AppendUpsert(entity, entity.Id, payloads)`
* **Bulk Delete:** `db.AppendDelete(entity, entity.Id, payloads)`

```csharp
var payloads = new[]
{
    new OrderUpdatePayload { Id = 101, CustomerId = 500 },
    new OrderUpdatePayload { Id = 102, CustomerId = 501 }
};

// Automatically utilizes internal cached templates for batch execution
var query = db.AppendUpsert(o, o.Id, payloads).Build();
```