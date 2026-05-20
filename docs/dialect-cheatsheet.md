# SqlInterpol Dialect Cheat-Sheet

All features below are written once using SqlInterpol's unified API. The library automatically translates to the correct syntax for each dialect.

---

## Identifier Quoting

| Dialect | Style | Example |
|---|---|---|
| SQL Server | Square brackets | `[Products]` |
| PostgreSQL | Double quotes | `"Products"` |
| MySQL | Backticks | `` `Products` `` |
| SQLite | Double quotes | `"Products"` |
| Oracle | Double quotes | `"Products"` |

---

## Parameter Placeholders

| Dialect | Style | Example |
|---|---|---|
| SQL Server | Named `@p` | `@p0, @p1, @p2` |
| PostgreSQL | Positional `$` | `$1, $2, $3` |
| MySQL | Named `@p` | `@p0, @p1, @p2` |
| SQLite | Positional `?` | `?0, ?1, ?2` |
| Oracle | Named `:` | `:p0, :p1, :p2` |

---

## Paging (`LIMIT` / `OFFSET`)

Write once (PostgreSQL-style canonical syntax):
```csharp
db.Append($$"""
    SELECT ...
    LIMIT {{limit}} OFFSET {{offset}}
""");
```

| Dialect | Generated SQL |
|---|---|
| SQL Server | `OFFSET 40 ROWS FETCH NEXT 20 ROWS ONLY` |
| PostgreSQL | `LIMIT 20 OFFSET 40` |
| MySQL | `LIMIT 20 OFFSET 40` |
| SQLite | `LIMIT 20 OFFSET 40` |
| Oracle | `OFFSET 40 ROWS FETCH NEXT 20 ROWS ONLY` |

---

## Row Locking (`FOR UPDATE` / `FOR SHARE`)

Write once (ANSI-style canonical syntax):
```csharp
db.Append($$"""
    SELECT ... FROM {{p}}
    FOR UPDATE
""");
```

| Dialect | `FOR UPDATE` | `FOR SHARE` |
|---|---|---|
| SQL Server | `WITH (UPDLOCK)` (inline hint) | `WITH (ROWLOCK, HOLDLOCK)` |
| PostgreSQL | `FOR UPDATE` (appended to end) | `FOR SHARE` (appended to end) |
| MySQL | `FOR UPDATE` (appended to end) | `FOR SHARE` (appended to end) |
| SQLite | *(throws `SqlDialectException`)* | *(throws `SqlDialectException`)* |
| Oracle | `FOR UPDATE` (appended to end) | *(throws `SqlDialectException`)* |

> **SQL Server note:** The lock hint is placed inline on the `FROM` clause (`FROM [Products] WITH (UPDLOCK)`), which is why placement differs from other dialects.

---

## `RETURNING` / `OUTPUT`

Write once (PostgreSQL-style canonical syntax):
```csharp
db.Append($$"""
    INSERT INTO {{p}} (...)
    VALUES (...)
    RETURNING {{p[x => x.Id]}}
""");
```

| Dialect | Generated SQL |
|---|---|
| SQL Server | `OUTPUT inserted.[Id]` *(emulated)* |
| PostgreSQL | `RETURNING "Id"` |
| MySQL | *(not supported — triggers `UnsupportedDialectFeatureAnalyzer` warning)* |
| SQLite | `RETURNING "Id"` |
| Oracle | `RETURNING "Id" INTO :p0` |

---

## Upsert (`ON CONFLICT`)

Write once (PostgreSQL-style canonical syntax):
```csharp
db.Append($$"""
    INSERT INTO {{p}} {{newProduct}}
    ON CONFLICT {{p[x => x.Id]}}
    DO UPDATE SET {{updateProduct}}
    """);
```

| Dialect | Generated SQL |
|---|---|
| SQL Server | `MERGE INTO [Products] USING ... ON (...) WHEN MATCHED THEN UPDATE SET ...` *(emulated)* |
| PostgreSQL | `ON CONFLICT (...) DO UPDATE SET ...` *(native)* |
| MySQL | `ON DUPLICATE KEY UPDATE ...` *(emulated)* |
| SQLite | `ON CONFLICT (...) DO UPDATE SET ...` *(native)* |
| Oracle | *(not supported — triggers `UnsupportedDialectFeatureAnalyzer` warning)* |

---

## `EXCEPT` / Set Operations

Write once:
```csharp
db.Append($$"""
    SELECT {{p[x => x.Id]}} FROM {{p}}
    EXCEPT
    SELECT {{p[x => x.Id]}} FROM {{p}} WHERE {{p[x => x.CategoryId]}} = {{excludedId}}
    """);
```

| Dialect | `EXCEPT` |
|---|---|
| SQL Server | `EXCEPT` |
| PostgreSQL | `EXCEPT` |
| MySQL | `EXCEPT` |
| SQLite | `EXCEPT` |
| Oracle | `MINUS` *(auto-translated)* |

---

## Multi-Table `DELETE`

Write once (ANSI-style):
```csharp
db.Append($"DELETE FROM {{p}} USING {{j}} WHERE {{p[x => x.Id]}} = {{j[x => x.ProductId]}}");
```

| Dialect | Strategy |
|---|---|
| SQL Server | Standard `DELETE ... FROM` join |
| PostgreSQL | `DELETE FROM ... USING ...` *(native)* |
| MySQL | `DELETE alias FROM target AS alias, joined WHERE ...` |
| SQLite | Standard `DELETE ... FROM` |
| Oracle | `DELETE FROM ... WHERE EXISTS (SELECT 1 FROM ... WHERE ...)` *(emulated)* |

---

## Multi-Table `UPDATE`

Write once (PostgreSQL-style canonical syntax):
```csharp
db.Append($$"""
    UPDATE {{p}}
    SET {{p[x => x.Price]}} = {{newPrice}}
    FROM {{c}} AS c1
    WHERE {{p[x => x.CategoryId]}} = c1.Id
    """);
```

| Dialect | Strategy |
|---|---|
| SQL Server | Standard `UPDATE ... SET ... FROM` |
| PostgreSQL | `UPDATE ... SET ... FROM ...` *(native)* |
| MySQL | `UPDATE target, joined SET ... WHERE ...` *(comma-join style)* |
| SQLite | Standard `UPDATE ... SET ... FROM` |
| Oracle | `MERGE INTO ... USING ... ON (...) WHEN MATCHED THEN UPDATE SET ...` *(emulated)* |

---

## Feature Support Matrix

| Feature | SQL Server | PostgreSQL | MySQL | SQLite | Oracle |
|---|:---:|:---:|:---:|:---:|:---:|
| `FOR UPDATE` | ✅ | ✅ | ✅ | ❌ | ✅ |
| `FOR SHARE` | ✅ | ✅ | ✅ | ❌ | ❌ |
| `RETURNING` | ✅ | ✅ | ❌ | ✅ | ✅ |
| `ON CONFLICT` (Upsert) | ✅ | ✅ | ✅ | ✅ | ❌ |

**Legend:** ✅ Supported (native or emulated) · ❌ Not supported (throws `SqlDialectException`)
