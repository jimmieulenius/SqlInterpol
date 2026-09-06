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

`SqlInterpol` handles vendor-specific escaping and parameter marker formatting automatically during compilation.

### Identifier Quoting

| Dialect | Style | Example Output |
| :--- | :--- | :--- |
| **Firebird** | Double quotes | `"Products"` |
| **MySQL** | Backticks | `` `Products` `` |
| **Oracle** | Double quotes | `"Products"` |
| **PostgreSQL** | Double quotes | `"Products"` |
| **SQL Server** | Square brackets | `[Products]` |
| **SQLite** | Double quotes | `"Products"` |

### Parameter Placeholders

| Dialect | Style | Example Output |
| :--- | :--- | :--- |
| **Firebird** | Named `@p` | `@p0, @p1, @p2` |
| **MySQL** | Named `@p` | `@p0, @p1, @p2` |
| **Oracle** | Named `:` | `:p0, :p1, :p2` |
| **PostgreSQL** | Positional `$` | `$1, $2, $3` |
| **SQL Server** | Named `@p` | `@p0, @p1, @p2` |
| **SQLite** | Positional `?` | `?0, ?1, ?2` |

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

#### Transpiled Outputs

*   **Firebird (3.0+):**
    ```sql
    SELECT "p"."Id", "p"."Name"
    FROM "Products" "p"
    ORDER BY "p"."CreatedAt" DESC
    OFFSET @p1 ROWS FETCH NEXT @p0 ROWS ONLY
    ```

*   **MySQL:**
    ```sql
    SELECT p.`Id`, p.`Name`
    FROM `Products` p
    ORDER BY p.`CreatedAt` DESC
    LIMIT @p0 OFFSET @p1
    ```

*   **Oracle (12c+):**
    ```sql
    SELECT "p"."Id", "p"."Name"
    FROM "Products" "p"
    ORDER BY "p"."CreatedAt" DESC
    OFFSET :p1 ROWS FETCH NEXT :p0 ROWS ONLY
    ```

*   **PostgreSQL:**
    ```sql
    SELECT p."Id", p."Name"
    FROM "Products" p
    ORDER BY p."CreatedAt" DESC
    LIMIT $1 OFFSET $2
    ```

*   **SQL Server:**
    ```sql
    SELECT [p].[Id], [p].[Name]
    FROM [Products] [p]
    ORDER BY [p].[CreatedAt] DESC
    OFFSET @p1 ROWS FETCH NEXT @p0 ROWS ONLY
    ```

*   **SQLite:**
    ```sql
    SELECT p."Id", p."Name"
    FROM "Products" p
    ORDER BY p."CreatedAt" DESC
    LIMIT ?0 OFFSET ?1
    ```

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
| **Firebird** | `WITH LOCK` *(appended to end)* | *(throws `SqlDialectException`)* |
| **MySQL** | `FOR UPDATE` *(appended to end)* | `FOR SHARE` *(appended to end)* |
| **Oracle** | `FOR UPDATE` *(appended to end)* | *(throws `SqlDialectException`)* |
| **PostgreSQL** | `FOR UPDATE` *(appended to end)* | `FOR SHARE` *(appended to end)* |
| **SQL Server** | `WITH (UPDLOCK)` *(inline table hint)* | `WITH (ROWLOCK, HOLDLOCK)` |
| **SQLite** | *(throws `SqlDialectException`)* | *(throws `SqlDialectException`)* |

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

#### Transpiled Outputs

*   **Firebird:**
    ```sql
    INSERT INTO "Products" ("Name", "Price")
    VALUES (@p0, @p1)
    RETURNING "Id"
    ```

*   **MySQL:** *(not supported — triggers `UnsupportedDialectFeatureAnalyzer` warning)*

*   **Oracle:**
    ```sql
    INSERT INTO "Products" ("Name", "Price")
    VALUES (:p0, :p1)
    RETURNING "Id" INTO :p2
    ```

*   **PostgreSQL:**
    ```sql
    INSERT INTO "Products" ("Name", "Price")
    VALUES ($1, $2)
    RETURNING "Id"
    ```

*   **SQL Server:**
    ```sql
    INSERT INTO [Products] ([Name], [Price])
    OUTPUT inserted.[Id]
    VALUES (@p0, @p1)
    ```

*   **SQLite:**
    ```sql
    INSERT INTO "Products" ("Name", "Price")
    VALUES (?0, ?1)
    RETURNING "Id"
    ```

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

#### Transpiled Outputs

*   **Firebird:**
    ```sql
    UPDATE OR INSERT INTO "Products" ("Id", "Name")
    VALUES (@p0, @p1)
    MATCHING ("Id")
    ```

*   **MySQL:**
    ```sql
    INSERT INTO `Products` (`Id`, `Name`)
    VALUES (@p0, @p1)
    ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name`)
    ```

*   **Oracle:** *(not supported — triggers `UnsupportedDialectFeatureAnalyzer` warning)*

*   **PostgreSQL:**
    ```sql
    INSERT INTO "Products" ("Id", "Name")
    VALUES ($1, $2)
    ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name"
    ```

*   **SQL Server:**
    ```sql
    MERGE INTO [Products] [target]
    USING (VALUES (@p0, @p1)) [source] ([Id], [Name])
    ON [target].[Id] = [source].[Id]
    WHEN MATCHED THEN
        UPDATE SET [Name] = [source].[Name]
    WHEN NOT MATCHED THEN
        INSERT ([Id], [Name]) VALUES ([source].[Id], [source].[Name]);
    ```

*   **SQLite:**
    ```sql
    INSERT INTO "Products" ("Id", "Name")
    VALUES (?0, ?1)
    ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name"
    ```

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

#### Transpiled Outputs

*   **Firebird:**
    ```sql
    SELECT "p"."Id" FROM "Products" "p"
    EXCEPT
    SELECT "p"."Id" FROM "Products" "p" WHERE "p"."CategoryId" = @p0
    ```

*   **MySQL:**
    ```sql
    SELECT p.`Id` FROM `Products` p
    EXCEPT
    SELECT p.`Id` FROM `Products` p WHERE p.`CategoryId` = @p0
    ```

*   **Oracle:**
    ```sql
    SELECT "p"."Id" FROM "Products" "p"
    MINUS
    SELECT "p"."Id" FROM "Products" "p" WHERE "p"."CategoryId" = :p0
    ```

*   **PostgreSQL:**
    ```sql
    SELECT p."Id" FROM "Products" p
    EXCEPT
    SELECT p."Id" FROM "Products" p WHERE p."CategoryId" = $1
    ```

*   **SQL Server:**
    ```sql
    SELECT [p].[Id] FROM [Products] [p]
    EXCEPT
    SELECT [p].[Id] FROM [Products] [p] WHERE [p].[CategoryId] = @p0
    ```

*   **SQLite:**
    ```sql
    SELECT p."Id" FROM "Products" p
    EXCEPT
    SELECT p."Id" FROM "Products" p WHERE p."CategoryId" = ?0
    ```

---

### 6. Multi-Table `DELETE`

Write using ANSI-style joins:

```csharp
db.Entity<Product>(out var p)
  .Entity<Category>(out var c);

db.Append($"""
    DELETE FROM {p} 
    USING {c} 
    WHERE {p.CategoryId} = {c.Id} AND {c.IsArchived} = {true}
    """);
```

#### Transpiled Outputs

*   **Firebird:**
    ```sql
    DELETE FROM "Products" "p"
    WHERE EXISTS (
        SELECT 1 FROM "Categories" "c"
        WHERE "p"."CategoryId" = "c"."Id" AND "c"."IsArchived" = @p0
    )
    ```

*   **MySQL:**
    ```sql
    DELETE `p`
    FROM `Products` `p`, `Categories` `c`
    WHERE `p`.`CategoryId` = `c`.`Id` AND `c`.`IsArchived` = @p0
    ```

*   **Oracle:**
    ```sql
    DELETE FROM "Products" "p"
    WHERE EXISTS (
        SELECT 1 FROM "Categories" "c"
        WHERE "p"."CategoryId" = "c"."Id" AND "c"."IsArchived" = :p0
    )
    ```

*   **PostgreSQL:**
    ```sql
    DELETE FROM "Products" p
    USING "Categories" c
    WHERE p."CategoryId" = c."Id" AND c."IsArchived" = $1
    ```

*   **SQL Server:**
    ```sql
    DELETE [p]
    FROM [Products] [p]
    INNER JOIN [Categories] [c] ON [p].[CategoryId] = [c].[Id]
    WHERE [c].[IsArchived] = @p0
    ```

*   **SQLite:**
    ```sql
    DELETE [p]
    FROM "Products" "p"
    INNER JOIN "Categories" "c" ON "p"."CategoryId" = "c"."Id"
    WHERE "c"."IsArchived" = ?0
    ```

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

#### Transpiled Outputs

*   **Firebird:**
    ```sql
    MERGE INTO "Products" "p"
    USING "Categories" "c"
    ON ("p"."CategoryId" = "c"."Id")
    WHEN MATCHED THEN
        UPDATE SET "p"."Price" = @p0
        WHERE "c"."IsArchived" = @p1
    ```

*   **MySQL:**
    ```sql
    UPDATE `Products` `p`, `Categories` `c`
    SET `p`.`Price` = @p0
    WHERE `p`.`CategoryId` = `c`.`Id` AND `c`.`IsArchived` = @p1
    ```

*   **Oracle:**
    ```sql
    MERGE INTO "Products" "p"
    USING "Categories" "c"
    ON ("p"."CategoryId" = "c"."Id")
    WHEN MATCHED THEN
        UPDATE SET "p"."Price" = :p0
        WHERE "c"."IsArchived" = :p1
    ```

*   **PostgreSQL:**
    ```sql
    UPDATE "Products" p
    SET "Price" = $1
    FROM "Categories" c
    WHERE p."CategoryId" = c."Id" AND c."IsArchived" = $2
    ```

*   **SQL Server:**
    ```sql
    UPDATE [p]
    SET [p].[Price] = @p0
    FROM [Products] [p]
    INNER JOIN [Categories] [c] ON [p].[CategoryId] = [c].[Id]
    WHERE [c].[IsArchived] = @p1
    ```

*   **SQLite:**
    ```sql
    UPDATE "p"
    SET "p"."Price" = ?0
    FROM "Products" "p"
    INNER JOIN "Categories" "c" ON "p"."CategoryId" = "c"."Id"
    WHERE "c"."IsArchived" = ?1
    ```

---

## Feature Support Matrix

| Feature | Firebird | MySQL | Oracle | PostgreSQL | SQL Server | SQLite |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **`LIMIT / OFFSET` Transpilation** | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| **Multi-Table DML Normalization** | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| **`FOR UPDATE` Lock Hints** | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ❌ |
| **`FOR SHARE` Lock Hints** | ❌ | ✔️ | ❌ | ✔️ | ✔️ | ❌ |
| **`RETURNING` / `OUTPUT`** | ✔️ | ❌ | ✔️ | ✔️ | ✔️ | ✔️ |
| **`ON CONFLICT` (Upsert)** | ✔️ | ✔️ | ❌ | ✔️ | ✔️ | ✔️ |

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