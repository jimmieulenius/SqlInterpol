# XV-SQL (Cross-Vendor SQL) Reference Guide

**XV-SQL (Cross-Vendor SQL)** is the unified, baseline syntax standard used by `SqlInterpol`. Instead of memorizing dialect-specific quirks, complex vendor lock-in mechanics, or emulated functions for every database, you simply write standard ANSI/PostgreSQL-style SQL. 

The `SqlInterpol` compiler engine intercepts your XV-SQL at runtime and structurally transpiles it into the highly-specific, optimized syntax required by your active database provider with **zero allocations** on the happy path.

>**Note**  
>In the examples below, assume `{{products}}` evaluates to a `Products` table, `{{categories}}` evaluates to a >`Categories` table, and `{{users}}` evaluates to a `Users` table.

---

## 1. Native Operators & Booleans
Safely filter by boolean values and concatenate strings without worrying about dialect-specific functions or types. The lexical preprocessor automatically swaps them for dialects that lack native support, perfectly preserving your formatting.

**XV-SQL:**
```csharp
db.Append($$"""
    SELECT FirstName || ' ' || LastName AS FullName
    FROM {{users}} 
    WHERE IsActive = TRUE
      AND IsDeleted = FALSE
""");
```

<details>
<summary><b>Firebird</b></summary>

```sql
SELECT "FirstName" || ' ' || "LastName" AS "FullName"
FROM "Users" 
WHERE "IsActive" = TRUE
  AND "IsDeleted" = FALSE
```
</details>

<details>
<summary><b>MySQL</b></summary>

```sql
SELECT `FirstName` || ' ' || `LastName` AS `FullName`
FROM `Users` 
WHERE `IsActive` = TRUE
  AND `IsDeleted` = FALSE
```

> **Note**  
>To use `||` natively in MySQL without modifying server settings, simply append >`SessionVariables=sql_mode='PIPES_AS_CONCAT';` to your `.NET` connection string.
</details>

<details>
<summary><b>Oracle</b></summary>

```sql
SELECT "FirstName" || ' ' || "LastName" AS "FullName"
FROM "Users" 
WHERE "IsActive" = 1
  AND "IsDeleted" = 0
```
</details>

<details>
<summary><b>PostgreSQL</b></summary>

```sql
SELECT "FirstName" || ' ' || "LastName" AS "FullName"
FROM "Users" 
WHERE "IsActive" = TRUE
  AND "IsDeleted" = FALSE
```
</details>

<details>
<summary><b>SQLite</b></summary>

```sql
SELECT "FirstName" || ' ' || "LastName" AS "FullName"
FROM "Users" 
WHERE "IsActive" = TRUE
  AND "IsDeleted" = FALSE
```
</details>

<details>
<summary><b>SQL Server</b></summary>

```sql
SELECT [FirstName] + ' ' + [LastName] AS [FullName]
FROM [Users] 
WHERE [IsActive] = 1
  AND [IsDeleted] = 0
```
</details>

---

## 2. Pagination (`LIMIT` / `OFFSET`)
Write your pagination logic using standard PostgreSQL `LIMIT/OFFSET` syntax. The AST rewriter intercepts these tokens and replaces them inline.

**XV-SQL:**
```csharp
db.Append($$"""
    SELECT *
    FROM {{products}}
    ORDER BY {{products.Price}} DESC
    LIMIT {{20}} OFFSET {{40}}
""");
```

<details>
<summary><b>Firebird</b></summary>

```sql
SELECT *
FROM "Products"
ORDER BY "Price" DESC
OFFSET @p1 ROWS FETCH NEXT @p0 ROWS ONLY
```
</details>

<details>
<summary><b>MySQL</b></summary>

```sql
SELECT *
FROM `Products`
ORDER BY `Price` DESC
LIMIT @p0 OFFSET @p1
```
</details>

<details>
<summary><b>Oracle</b></summary>

```sql
SELECT *
FROM "Products"
ORDER BY "Price" DESC
OFFSET :p1 ROWS FETCH NEXT :p0 ROWS ONLY
```
</details>

<details>
<summary><b>PostgreSQL</b></summary>

```sql
SELECT *
FROM "Products"
ORDER BY "Price" DESC
LIMIT $1 OFFSET $2
```
</details>

<details>
<summary><b>SQLite</b></summary>

```sql
SELECT *
FROM "Products"
ORDER BY "Price" DESC
LIMIT @p0 OFFSET @p1
```
</details>

<details>
<summary><b>SQL Server</b></summary>

```sql
SELECT *
FROM [Products]
ORDER BY [Price] DESC
OFFSET @p1 ROWS FETCH NEXT @p0 ROWS ONLY
```
</details>

---

## 3. Row Locking (`FOR UPDATE` / `FOR SHARE`)
Write ANSI-style locking hints at the end of your query. For databases like SQL Server, the engine intelligently cleans up the trailing keyword and hoists the hint inline.

**XV-SQL:**
```csharp
db.Append($$"""
    SELECT *
    FROM {{products}}
    WHERE {{products.Id}} = {{id}}
    FOR UPDATE
""");
```

<details>
<summary><b>Firebird</b></summary>

```sql
SELECT *
FROM "Products"
WHERE "Id" = @p0
FOR UPDATE
```
</details>

<details>
<summary><b>MySQL</b></summary>

```sql
SELECT *
FROM `Products`
WHERE `Id` = @p0
FOR UPDATE
```
</details>

<details>
<summary><b>Oracle</b></summary>

```sql
SELECT *
FROM "Products"
WHERE "Id" = :p0
FOR UPDATE
```
</details>

<details>
<summary><b>PostgreSQL</b></summary>

```sql
SELECT *
FROM "Products"
WHERE "Id" = $1
FOR UPDATE
```
</details>

<details>
<summary><b>SQL Server</b></summary>

```sql
SELECT *
FROM [Products] WITH (UPDLOCK)
WHERE [Id] = @p0
```
</details>

---

## 4. Returning Data (`RETURNING` / `OUTPUT`)
To retrieve generated IDs or modified rows during DML operations, use the standard `RETURNING` keyword.

**XV-SQL:**
```csharp
db.Append($$"""
    INSERT INTO {{products}} (Name, Price) 
    VALUES ({{name}}, {{price}})
    RETURNING {{products.Id}}
""");
```

<details>
<summary><b>Firebird</b></summary>

```sql
INSERT INTO "Products" ("Name", "Price") 
VALUES (@p0, @p1)
RETURNING "Id"
```
</details>

<details>
<summary><b>Oracle</b></summary>

```sql
INSERT INTO "Products" ("Name", "Price") 
VALUES (:p0, :p1)
RETURNING "Id" INTO :p2
```
</details>

<details>
<summary><b>PostgreSQL</b></summary>

```sql
INSERT INTO "Products" ("Name", "Price") 
VALUES ($1, $2)
RETURNING "Id"
```
</details>

<details>
<summary><b>SQLite</b></summary>

```sql
INSERT INTO "Products" ("Name", "Price") 
VALUES (@p0, @p1)
RETURNING "Id"
```
</details>

<details>
<summary><b>SQL Server</b></summary>

```sql
INSERT INTO [Products] (Name, Price) 
OUTPUT inserted.[Id]
VALUES (@p0, @p1)
```
</details>

---

## 5. Upsert (`ON CONFLICT`)
Write your upserts using PostgreSQL's robust, conflict-resolution syntax. The engine handles complex structural translations, such as generating SQL Server `MERGE` statements on the fly.

**XV-SQL:**
```csharp
db.Append($$"""
    INSERT INTO {{products}} (Id, Name, Price) 
    VALUES ({{1}}, {{'Widget'}}, {{9.99}})
    ON CONFLICT ({{products.Id}}) 
    DO UPDATE SET Price = {{9.99}}
""");
```

<details>
<summary><b>Firebird</b></summary>

```sql
UPDATE OR INSERT INTO "Products" ("Id", "Name", "Price") 
VALUES (@p0, @p1, @p2) 
MATCHING ("Id")
```
</details>

<details>
<summary><b>MySQL</b></summary>

```sql
INSERT INTO `Products` (`Id`, `Name`, `Price`) 
VALUES (@p0, @p1, @p2)
ON DUPLICATE KEY UPDATE `Price` = @p3
```
</details>

<details>
<summary><b>PostgreSQL</b></summary>

```sql
INSERT INTO "Products" ("Id", "Name", "Price") 
VALUES ($1, $2, $3)
ON CONFLICT ("Id") 
DO UPDATE SET "Price" = $4
```
</details>

<details>
<summary><b>SQLite</b></summary>

```sql
INSERT INTO "Products" ("Id", "Name", "Price") 
VALUES (@p0, @p1, @p2)
ON CONFLICT ("Id") 
DO UPDATE SET "Price" = @p3
```
</details>

<details>
<summary><b>SQL Server</b></summary>

```sql
MERGE INTO [Products] AS [tgt] 
USING (SELECT @p0 AS [Id], @p1 AS [Name], @p2 AS [Price]) AS [src] 
ON [tgt].[Id] = [src].[Id] 
WHEN MATCHED THEN UPDATE SET [Price] = @p3 
WHEN NOT MATCHED THEN INSERT ([Id], [Name], [Price]) VALUES ([src].[Id], [src].[Name], [src].[Price]);
```
</details>

---

## 6. Set Operations (`EXCEPT`)
Perform standard set subtractions. XV-SQL will automatically translate keywords for databases that don't follow the ANSI standard.

**XV-SQL:**
```csharp
db.Append($$"""
    SELECT {{products.Id}}
    FROM {{products}}
    EXCEPT
    SELECT {{products.Id}}
    FROM {{products}}
    WHERE {{products.Price}} > {{100}}
""");
```

<details>
<summary><b>Firebird</b></summary>

```sql
SELECT "Id"
FROM "Products"
EXCEPT
SELECT "Id"
FROM "Products"
WHERE "Price" > @p0
```
</details>

<details>
<summary><b>MySQL</b></summary>

```sql
SELECT `Id`
FROM `Products`
EXCEPT
SELECT `Id`
FROM `Products`
WHERE `Price` > @p0
```
</details>

<details>
<summary><b>Oracle</b></summary>

```sql
SELECT "Id"
FROM "Products"
MINUS
SELECT "Id"
FROM "Products"
WHERE "Price" > :p0
```
</details>

<details>
<summary><b>PostgreSQL</b></summary>

```sql
SELECT "Id"
FROM "Products"
EXCEPT
SELECT "Id"
FROM "Products"
WHERE "Price" > $1
```
</details>

<details>
<summary><b>SQLite</b></summary>

```sql
SELECT "Id"
FROM "Products"
EXCEPT
SELECT "Id"
FROM "Products"
WHERE "Price" > @p0
```
</details>

<details>
<summary><b>SQL Server</b></summary>

```sql
SELECT [Id]
FROM [Products]
EXCEPT
SELECT [Id]
FROM [Products]
WHERE [Price] > @p0
```
</details>

---

## 7. Multi-Table `UPDATE`
Write standard `UPDATE ... FROM` queries to perform cross-table mutations based on JOINs.

**XV-SQL:**
```csharp
db.Append($$"""
    UPDATE {{products}}
    SET Price = {{newPrice}}
    FROM {{categories}} AS c
    WHERE {{products.CategoryId}} = c.Id
      AND c.IsActive = TRUE
""");
```

<details>
<summary><b>Firebird</b></summary>

```sql
MERGE INTO "Products" "tgt" 
USING "Categories" "c" 
  ON "tgt"."CategoryId" = "c"."Id"
  AND "c"."IsActive" = TRUE
WHEN MATCHED THEN UPDATE SET "Price" = @p0
```
</details>

<details>
<summary><b>MySQL</b></summary>

```sql
UPDATE
  `Products`,
  `Categories` AS `c`
SET `Price` = @p0
WHERE `Products`.`CategoryId` = `c`.`Id`
  AND `c`.`IsActive` = TRUE
```
</details>

<details>
<summary><b>Oracle</b></summary>

```sql
MERGE INTO "Products" "tgt" 
USING "Categories" "c" 
  ON "tgt"."CategoryId" = "c"."Id"
  AND "c"."IsActive" = 1
WHEN MATCHED THEN UPDATE SET "Price" = :p0
```
</details>

<details>
<summary><b>PostgreSQL</b></summary>

```sql
UPDATE "Products"
SET "Price" = $1
FROM "Categories" AS "c"
WHERE "Products"."CategoryId" = "c"."Id"
  AND "c"."IsActive" = TRUE
```
</details>

<details>
<summary><b>SQLite</b></summary>

```sql
UPDATE "Products"
SET "Price" = @p0
FROM "Categories" AS "c"
WHERE "Products"."CategoryId" = "c"."Id"
  AND "c"."IsActive" = TRUE
```
</details>

<details>
<summary><b>SQL Server</b></summary>

```sql
UPDATE [tgt]
SET [Price] = @p0
FROM [Products] AS [tgt]
INNER JOIN [Categories] AS [c]
  ON [tgt].[CategoryId] = [c].[Id]
WHERE [Products].[CategoryId] = [c].[Id]
  AND [c].[IsActive] = 1
```
</details>

---

## 8. Multi-Table `DELETE`
Write standard `DELETE ... FROM ... USING` queries to safely delete rows based on a filtered joined table.

**XV-SQL:**
```csharp
db.Append($$"""
    DELETE FROM {{products}}
    USING {{categories}} AS c
    WHERE {{products.CategoryId}} = c.Id
      AND c.IsActive = FALSE
""");
```

<details>
<summary><b>Firebird</b></summary>

```sql
DELETE FROM "Products"
WHERE EXISTS (
  SELECT 1
  FROM "Categories" "c"
  WHERE "Products"."CategoryId" = "c"."Id"
    AND "c"."IsActive" = FALSE
)
```
</details>

<details>
<summary><b>MySQL</b></summary>

```sql
DELETE `tgt`
FROM `Products` AS `tgt`
INNER JOIN `Categories` AS `c`
  ON `tgt`.`CategoryId` = `c`.`Id`
WHERE `Products`.`CategoryId` = `c`.`Id`
  AND `c`.`IsActive` = FALSE
```
</details>

<details>
<summary><b>Oracle</b></summary>

```sql
DELETE FROM "Products"
WHERE EXISTS (
  SELECT 1
  FROM "Categories" "c"
  WHERE "Products"."CategoryId" = "c"."Id"
    AND "c"."IsActive" = 0
)
```
</details>

<details>
<summary><b>PostgreSQL</b></summary>

```sql
DELETE FROM "Products"
USING "Categories" AS "c"
WHERE "Products"."CategoryId" = "c"."Id"
  AND "c"."IsActive" = FALSE
```
</details>

<details>
<summary><b>SQLite</b></summary>

```sql
DELETE FROM "Products"
FROM "Categories" AS "c"
WHERE "Products"."CategoryId" = "c"."Id"
  AND "c"."IsActive" = FALSE
```
</details>

<details>
<summary><b>SQL Server</b></summary>

```sql
DELETE [tgt]
FROM [Products] AS [tgt]
INNER JOIN [Categories] AS [c]
  ON [tgt].[CategoryId] = [c].[Id]
WHERE [Products].[CategoryId] = [c].[Id]
  AND [c].[IsActive] = 0
```
</details>

---

## Framework Mechanics (Auto-Handled)

SqlInterpol completely abstracts mechanical dialect differences away from the developer. You simply pass in your POCO models and interpolated variables, and let the engine handle the rest.

| Concept | Firebird | MySQL | Oracle | PostgreSQL | SQLite | SQL Server |
|---|---|---|---|---|---|---|
| **Identifier Quoting** | `"Products"` | `` `Products` `` | `"Products"` | `"Products"` | `"Products"` | `[Products]` |
| **Parameter Placeholders**| `@p0, @p1` | `@p0, @p1` | `:p0, :p1` | `$1, $2` | `?0, ?1` | `@p0, @p1` |

---

## Feature Support Matrix

| XV-SQL Feature | Firebird | MySQL | Oracle | PostgreSQL | SQLite | SQL Server |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| **`TRUE` / `FALSE`** | ✅ | ✅ | ✅ *(1/0)* | ✅ | ✅ | ✅ *(1/0)* |
| **`\|\|` Concat** | ✅ | ✅ *(w/ flag)* | ✅ | ✅ | ✅ | ✅ *(+)* |
| **`LIMIT / OFFSET`** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **`FOR UPDATE`** | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| **`FOR SHARE`** | ❌ | ✅ | ❌ | ✅ | ❌ | ✅ |
| **`RETURNING`** | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ |
| **`ON CONFLICT`** | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ |
| **Multi-Table DML**| ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

**Legend:** ✅ Supported (native or structurally emulated) · ❌ Not supported by Database engine (throws safe `SqlDialectException`)