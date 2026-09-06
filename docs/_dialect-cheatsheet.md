# Cross-Dialect Transpilation & Dialect Cheat-Sheet

`SqlInterpol` includes a built-in compilation pipeline that automatically rewrites standard SQL syntax into database-specific idioms. Write your queries once using unified, canonical syntax, and the engine seamlessly translates identifiers, parameters, clauses, and DML operations for your target database.

## How It Works

During query compilation, the engine passes parsed query segments through a pipeline of **Rewriters**. These rewriters inspect the query structure for dialect-specific incompatibilities and adjust the query segments before generating final SQL string outputs.

```
Interpolated String  ──>  Lexer & Preprocessor  ──>  Query Segments  ──>  Rewriter Pipeline  ──>  Dialect Renderer
```

Transpilation can be toggled globally or per-instance via the `CrossDialectSqlTranspilation` setting in `SqlInterpolOptions`.

---

## Identifiers & Parameters

`SqlInterpol` handles vendor-specific escaping and parameter marker format automatically during compilation.

### Identifier Quoting

| Dialect | Style | Example Output |
| :--- | :--- | :--- |
| **SQL Server** | Square brackets | `[Products]` |
| **PostgreSQL** | Double quotes | `"Products"` |
| **MySQL** | Backticks | `` `Products` `` |
| **SQLite** | Double quotes | `"Products"` |
| **Oracle** | Double quotes | `"Products"` |

### Parameter Placeholders

| Dialect | Style | Example Output |
| :--- | :--- | :--- |
| **SQL Server** | Named `@p` | `@p0, @p1, @p2` |
| **PostgreSQL** | Positional `$` | `$1, $2, $3` |
| **MySQL** | Named `@p` | `@p0, @p1, @p2` |
| **SQLite** | Positional `?` | `?0, ?1, ?2` |
| **Oracle** | Named `:` | `:p0, :p1, :p2` |

---

## Transpilation Features

### 1. Paging (`LIMIT` / `OFFSET`)

Write using PostgreSQL-style canonical syntax:

```csharp
db.Append($"""
    SELECT {p.Id}, {p.Name}
    FROM {p}
    ORDER BY {p.CreatedAt} DESC
    LIMIT {limit} OFFSET {offset}
    """);
```

| Dialect | Generated SQL Output |
| :--- | :--- |
| **SQL Server** | `SELECT [p].[Id], [p].[Name] FROM [Products] [p] ORDER BY [p].[CreatedAt] DESC OFFSET 40 ROWS FETCH NEXT 20 ROWS ONLY` |
| **PostgreSQL** | `SELECT p."Id", p."Name" FROM "Products" p ORDER BY p."CreatedAt" DESC LIMIT 20 OFFSET 40` |
| **MySQL** | ``SELECT p.`Id`, p.`Name` FROM `Products` p ORDER BY p.`CreatedAt` DESC LIMIT 20 OFFSET 40`` |
| **SQLite** | `SELECT p."Id", p."Name" FROM "Products" p ORDER BY p."CreatedAt" DESC LIMIT 20 OFFSET 40` |
| **Oracle** | `SELECT "p"."Id", "p"."Name" FROM "Products" "p" ORDER BY "p"."CreatedAt" DESC OFFSET 40 ROWS FETCH NEXT 20 ROWS ONLY` |

> ⚠️ **SQL Server Order By Requirement**  
> SQL Server requires an explicit `ORDER BY` clause when using `OFFSET ... FETCH NEXT`. If no `ORDER BY` clause is present, `SqlInterpol` automatically injects `ORDER BY (SELECT NULL)` to prevent SQL Server runtime syntax errors.

---

### 2. Row Locking (`FOR UPDATE` / `FOR SHARE`)

Write using ANSI-style canonical syntax:

```csharp
db.Append($"""
    SELECT {p.Id}, {p.Price} 
    FROM {p}
    WHERE {p.Id} = {productId}
    FOR UPDATE
    """);
```

| Dialect | `FOR UPDATE` | `FOR SHARE` |
| :--- | :--- | :--- |
| **SQL Server** | `WITH (UPDLOCK)` *(inline table hint)* | `WITH (ROWLOCK, HOLDLOCK)` |
| **PostgreSQL** | `FOR UPDATE` *(appended to end)* | `FOR SHARE` *(appended to end)* |
| **MySQL** | `FOR UPDATE` *(appended to end)* | `FOR SHARE` *(appended to end)* |
| **SQLite** | *(throws `SqlDialectException`)* | *(throws `SqlDialectException`)* |
| **Oracle** | `FOR UPDATE` *(appended to end)* | *(throws `SqlDialectException`)* |

> ℹ️ **SQL Server Placement**  
> SQL Server lock hints are placed inline directly on the `FROM` clause (`FROM [Products] [p] WITH (UPDLOCK)`), which the compiler handles automatically during rendering.

---

### 3. `RETURNING` / `OUTPUT`

Write using PostgreSQL-style canonical syntax:

```csharp
db.Append($"""
    INSERT INTO {p} ({p.Name}, {p.Price})
    VALUES ({name}, {price})
    RETURNING {p.Id}
    """);
```

| Dialect | Generated SQL Output |
| :--- | :--- |
| **SQL Server** | `INSERT INTO [Products] ([Name], [Price]) OUTPUT inserted.[Id] VALUES (@p0, @p1)` *(emulated)* |
| **PostgreSQL** | `INSERT INTO "Products" ("Name", "Price") VALUES ($1, $2) RETURNING "Id"` |
| **MySQL** | *(not supported — triggers `UnsupportedDialectFeatureAnalyzer` warning)* |
| **SQLite** | `INSERT INTO "Products" ("Name", "Price") VALUES (?0, ?1) RETURNING "Id"` |
| **Oracle** | `INSERT INTO "Products" ("Name", "Price") VALUES (:p0, :p1) RETURNING "Id" INTO :p2` |

---

### 4. Upsert (`ON CONFLICT`)

Write using PostgreSQL-style canonical syntax:

```csharp
db.Append($"""
    INSERT INTO {p} {newProduct}
    ON CONFLICT {p.Id}
    DO UPDATE SET {updateProduct}
    """);
```

| Dialect | Strategy & Generated SQL |
| :--- | :--- |
| **SQL Server** | `MERGE INTO [Products] USING ... ON (...) WHEN MATCHED THEN UPDATE SET ...` *(emulated)* |
| **PostgreSQL** | `ON CONFLICT ("Id") DO UPDATE SET ...` *(native)* |
| **MySQL** | `ON DUPLICATE KEY UPDATE ...` *(emulated)* |
| **SQLite** | `ON CONFLICT ("Id") DO UPDATE SET ...` *(native)* |
| **Oracle** | *(not supported — triggers `UnsupportedDialectFeatureAnalyzer` warning)* |

---

### 5. `EXCEPT` / Set Operations

Write standard set operations across datasets:

```csharp
db.Append($"""
    SELECT {p.Id} FROM {p}
    EXCEPT
    SELECT {p.Id} FROM {p} WHERE {p.CategoryId} = {excludedId}
    """);
```

| Dialect | `EXCEPT` Output |
| :--- | :--- |
| **SQL Server** | `EXCEPT` |
| **PostgreSQL** | `EXCEPT` |
| **MySQL** | `EXCEPT` |
| **SQLite** | `EXCEPT` |
| **Oracle** | `MINUS` *(auto-translated)* |

---

### 6. Multi-Table `DELETE`

Write using ANSI-style joins:

```csharp
db.Entity<Product>(out var p)
  .Entity<Category>(out var c);

db.Append($"DELETE FROM {p} USING {c} WHERE {p.CategoryId} = {c.Id} AND {c.IsArchived} = {true}");
```

| Dialect | Strategy |
| :--- | :--- |
| **SQL Server** | Standard `DELETE ... FROM` join |
| **PostgreSQL** | `DELETE FROM ... USING ...` *(native)* |
| **MySQL** | `DELETE alias FROM target AS alias, joined WHERE ...` |
| **SQLite** | Standard `DELETE ... FROM` |
| **Oracle** | `DELETE FROM ... WHERE EXISTS (SELECT 1 FROM ... WHERE ...)` *(emulated)* |

---

### 7. Multi-Table `UPDATE`

Write using PostgreSQL-style canonical syntax:

```csharp
db.Entity<Product>(out var p)
  .Entity<Category>(out var c);

db.Append($"""
    UPDATE {p}
    SET {p.Price} = {newPrice}
    FROM {c}
    WHERE {p.CategoryId} = {c.Id} AND {c.IsArchived} = {false}
    """);
```

| Dialect | Strategy |
| :--- | :--- |
| **SQL Server** | Standard `UPDATE ... SET ... FROM` |
| **PostgreSQL** | `UPDATE ... SET ... FROM ...` *(native)* |
| **MySQL** | `UPDATE target, joined SET ... WHERE ...` *(comma-join style)* |
| **SQLite** | Standard `UPDATE ... SET ... FROM` |
| **Oracle** | `MERGE INTO ... USING ... ON (...) WHEN MATCHED THEN UPDATE SET ...` *(emulated)* |

---

## Feature Support Matrix

| Feature | SQL Server | PostgreSQL | MySQL | SQLite | Oracle |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **`LIMIT / OFFSET` Transpilation** | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| **Multi-Table DML Normalization** | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| **`FOR UPDATE` Lock Hints** | ✔️ | ✔️ | ✔️ | ❌ | ✔️ |
| **`FOR SHARE` Lock Hints** | ✔️ | ✔️ | ✔️ | ❌ | ❌ |
| **`RETURNING` / `OUTPUT`** | ✔️ | ✔️ | ❌ | ✔️ | ✔️ |
| **`ON CONFLICT` (Upsert)** | ✔️ | ✔️ | ✔️ | ✔️ | ❌ |

> **Legend:**  
> ✔️ **Supported:** Native database syntax or fully emulated by rewriter pipeline.  
> ❌ **Not Supported:** Unsupported by database driver (throws `SqlDialectException` or triggers analyzer warning).

---

## Disabling Transpilation

If you prefer to bypass compiler rewriters and pass raw, unmodified vendor SQL directly to your driver:

```csharp
var options = new SqlInterpolOptions
{
    CrossDialectSqlTranspilation = false
};

using var db = connection.CreateSqlBuilder(options);
```