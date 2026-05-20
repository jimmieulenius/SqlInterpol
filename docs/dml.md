# DML — INSERT, UPDATE, DELETE, UPSERT

SqlInterpol supports the full DML surface through the same interpolation model as SELECT queries. All values are automatically parameterized; all column and table names are quoted per-dialect.

## INSERT

Pass an anonymous type (or any POCO) directly into the interpolation to get a fully parameterized INSERT with auto-mapped columns:

```csharp
var db = dbConnection.CreateSqlBuilder();
var newProduct = new { Name = "Gadget Pro", CategoryId = 3, Price = 49.99m };

var query = db.Query<Product>(p => db.Append($$"""
    INSERT INTO {{p}}
    {{newProduct}}
    """)).Build();
```

**Generated SQL (PostgreSQL):**
```sql
INSERT INTO "Product" ("Name", "CategoryId", "Price")
VALUES ($1, $2, $3)
```

## Bulk INSERT

Pass a collection into `VALUES {{rows}}` and SqlInterpol generates a single multi-row statement:

```csharp
var rows = new[]
{
    new { Name = "Widget A", CategoryId = 1, Price = 9.99m },
    new { Name = "Widget B", CategoryId = 1, Price = 14.99m },
};

var query = db.Query<Product>(p => db.Append($$"""
    INSERT INTO {{p}} VALUES {{rows}}
    """)).Build();
```

**Generated SQL (PostgreSQL):**
```sql
INSERT INTO "Product" ("Name", "CategoryId", "Price")
VALUES ($1, $2, $3), ($4, $5, $6)
```

## UPDATE

### DTO-driven

Pass an anonymous type or POCO into `SET {{dto}}` and every property becomes a parameterized `col = @param` assignment:

```csharp
var patch = new { Status = "Shipped", Total = 99.99m };
int orderId = 42;

var query = db.Query<Order>(o => db.Append($$"""
    UPDATE {{o}}
    SET {{patch}}
    WHERE {{o[x => x.Id]}} = {{orderId}}
    """)).Build();
```

**Generated SQL (SQL Server):**
```sql
UPDATE [Orders]
SET [Status] = @p0, [Total] = @p1
WHERE [Orders].[Id] = @p2
```

### Explicit column-by-column

```csharp
var query = db.Query<Order>(o => db.Append($$"""
    UPDATE {{o}}
    SET {{o[x => x.Status]}} = {{"Shipped"}}, {{o[x => x.Total]}} = {{99.99m}}
    WHERE {{o[x => x.Id]}} = {{orderId}}
    """)).Build();
```

## DELETE

```csharp
int targetId = 42;

var query = db.Query<Order>(o => db.Append($$"""
    DELETE FROM {{o}}
    WHERE {{o[x => x.Id]}} = {{targetId}}
    """)).Build();
```

**Generated SQL (PostgreSQL):**
```sql
DELETE FROM "Orders"
WHERE "Orders"."Id" = $1
```

## UPSERT (ON CONFLICT / ON DUPLICATE KEY UPDATE / MERGE)

Write standard `ON CONFLICT` syntax once — SqlInterpol rewrites it for every dialect automatically:

```csharp
var newProduct = new { Id = 42, Name = "Apple", CategoryId = 1, Price = 10m };
var updateFields = new { Name = "Apple", Price = 10m };

var query = db.Query<Product>(p => db.Append($$"""
    INSERT INTO {{p}} {{newProduct}}
    ON CONFLICT {{p[x => x.Id]}}
    DO UPDATE SET {{updateFields}}
    """)).Build();
```

**Generated SQL (PostgreSQL):**
```sql
INSERT INTO "Product" ("Id", "Name", "CategoryId", "Price")
VALUES ($1, $2, $3, $4)
ON CONFLICT ("Id")
DO UPDATE SET "Name" = $5, "Price" = $6
```

**Generated SQL (MySQL):**
```sql
INSERT INTO `Product` (`Id`, `Name`, `CategoryId`, `Price`)
VALUES (@p0, @p1, @p2, @p3)
ON DUPLICATE KEY UPDATE `Name` = @p4, `Price` = @p5
```

**Generated SQL (SQL Server)** — rewritten to a full `MERGE` statement automatically:
```sql
MERGE INTO [Product] AS target
USING (VALUES (@p0, @p1, @p2, @p3)) AS source([Id], [Name], [CategoryId], [Price])
ON target.[Id] = source.[Id]
WHEN MATCHED THEN
  UPDATE SET target.[Name] = @p4, target.[Price] = @p5
WHEN NOT MATCHED THEN
  INSERT ([Id], [Name], [CategoryId], [Price])
  VALUES (source.[Id], source.[Name], source.[CategoryId], source.[Price]);
```

## RETURNING / OUTPUT

PostgreSQL / SQLite — `RETURNING` clause:

```csharp
var query = db.Query<Product>(p => db.Append($$"""
    INSERT INTO {{p}} {{newProduct}}
    RETURNING {{p[x => x.Id]}}
    """)).Build();
```

**Generated SQL (PostgreSQL):**
```sql
INSERT INTO "Product" ("Name", "CategoryId", "Price")
VALUES ($1, $2, $3)
RETURNING "Id"
```

**Generated SQL (SQL Server)** — `OUTPUT` clause rewritten automatically:
```sql
INSERT INTO [Product] ([Name], [CategoryId], [Price])
OUTPUT INSERTED.[Id]
VALUES (@p0, @p1, @p2)
```
