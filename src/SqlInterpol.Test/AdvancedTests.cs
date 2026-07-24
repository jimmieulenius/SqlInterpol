using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class AdvancedTests
{
    [Theory]
    [MemberData(nameof(DynamicQueryData))]
    public void DynamicQuery(SqlTestCase testCase)
    {
        var request = new GetOrderStatsRequest
        {
            CustomerId = 5,
            SelectFields = ["OrderId", "TotalAmount"],
            SortFields = [new SortCriteria("TotalAmount", true)],
            Page = 2,
            PageSize = 20
        };

        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();

            db.Entity<ApiOrderStatsModel>(out var stats, "stats")
              .Entity<OrderModel>(out var o, "o")
              .Entity<OrderLine>(out var ol, "ol");

            db.Append($"SELECT ");
            bool first = true;
            foreach (var f in request.SelectFields)
            {
                if (!first) db.Append($", ");
                db.Append($"{stats.Column(f)}");
                first = false;
            }

            db.AppendLine($"""
                
                FROM (
                    SELECT 
                        {o.CustomerId},
                        {o.Id} AS {ol.OrderId:alias}, 
                        SUM({ol.Price}) AS {stats.TotalAmount:alias}
                    FROM {o}
                    JOIN {ol} ON {o.Id} = {ol.OrderId}
                    GROUP BY {o.CustomerId}, {o.Id}
                ) AS {stats:alias}
                """);

            if (request.CustomerId.HasValue) 
                db.AppendLine($"WHERE {stats.CustomerId} = {request.CustomerId}"); 
            
            if (request.SortFields.Any()) 
            {
                db.Append($"ORDER BY ");
                first = true;
                foreach (var sort in request.SortFields)
                {
                    if (!first) db.Append($", ");
                    db.Append($"{stats.Column(sort.Field)} {(sort.Descending ? Sql.Raw("DESC") : Sql.Raw("ASC"))}");
                    first = false;
                }
                db.AppendLine($"");
            }

            int offset = (request.Page - 1) * request.PageSize;
            db.Append($"LIMIT {request.PageSize} OFFSET {offset}");

            return db.Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(AdvancedDynamicQueryData))]
    public void AdvancedDynamicQuery(SqlTestCase testCase)
    {
        var request = new GetMassiveStatsRequest
        {
            ProductNameFilter = "Laptop",
            SelectFields = ["OrderId", "ProductName", "TotalAmount"],
            SortFields = [new SortCriteria("TotalAmount", true)], 
            Page = 2, 
            PageSize = 20
        };

        testCase.Action(() =>
        {
            var db = testCase.CreateBuilder();
            
            db.Entity<MassiveOrderStatsModel>(out var stats, "stats")
              .Entity<OrderModel>(out var o, "o")
              .Entity<OrderLine>(out var ol, "ol")
              .Entity<Product>(out var p, "p")
              .Entity<Category>(out var cat, "cat")
              .Entity<OrderLineAggModel>(out var olAgg, "ol_agg");

            db.Append($"SELECT ");
            bool first = true;
            foreach (var f in request.SelectFields)
            {
                if (!first) db.Append($", ");
                db.Append($"{stats.Column(f)}");
                first = false;
            }

            db.AppendLine($"""
                
                FROM (
                    SELECT 
                        {o.Id} AS {stats.OrderId:alias}, 
                        {p.Name} AS {stats.ProductName:alias},
                        {olAgg.TotalAmount} AS {stats.TotalAmount:alias}
                    FROM {o}
                    
                    JOIN (
                        SELECT 
                            {ol.OrderId} AS {olAgg.OrderId:alias},
                            {ol.ProductId} AS {olAgg.ProductId:alias},
                            SUM({ol.Price}) AS {olAgg.TotalAmount:alias}
                        -- JOIN ol_agg ON ...
                        FROM {ol}
                        GROUP BY {ol.OrderId}, {ol.ProductId}
                    ) AS {olAgg:alias} ON {o.Id} = {olAgg.OrderId}
                    
                    JOIN {p} ON {olAgg.ProductId} = {p.Id}
                    JOIN {cat} ON {p.CategoryId} = {cat.Id}
                ) AS {stats:alias}
                """);

            if (!string.IsNullOrEmpty(request.ProductNameFilter)) 
                db.AppendLine($"WHERE {stats.ProductName} = {request.ProductNameFilter}"); 
            
            if (request.SortFields.Any()) 
            {
                db.Append($"ORDER BY ");
                first = true;
                foreach (var sort in request.SortFields)
                {
                    if (!first) db.Append($", ");
                    db.Append($"{stats.Column(sort.Field)} {(sort.Descending ? Sql.Raw("DESC") : Sql.Raw("ASC"))}");
                    first = false;
                }
                db.AppendLine($"");
            }

            int offset = (request.Page - 1) * request.PageSize;
            db.Append($"LIMIT {request.PageSize} OFFSET {offset}");

            return db.Build();
        });

        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(ComplexRawSqlData))]
    public void RawSql_ComplexStatements_PassThroughUnmodified(SqlTestCase testCase)
    {
        var minPrice = 50.00m;

        testCase.Action(() => 
        {
            var db = testCase.CreateBuilder();
            db.Entity<Product>(out var p);
            
            return db.Append($"""
                SELECT {p.Id}, {p.Name}
                FROM {p}
                WHERE {p.Price} > {minPrice}
                  AND p.Status = 'ACTIVE' /* Raw SQL condition */
                GROUP BY {p.Id}, {p.Name}
                HAVING COUNT(*) > 1
                ORDER BY {p.Name} DESC
                LIMIT 10 OFFSET 5
                """).Build();
        });

        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> DynamicQueryData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<stats>>.<<OrderId>>, <<stats>>.<<TotalAmount>>
                FROM (
                    SELECT 
                        <<o>>.<<CustomerId>>,
                        <<o>>.<<Id>> AS <<OrderId>>, 
                        SUM(<<ol>>.<<Price>>) AS <<TotalAmount>>
                    FROM <<dbo>>.<<Orders>> AS <<o>>
                    JOIN <<OrderLine>> AS <<ol>> ON <<o>>.<<Id>> = <<ol>>.<<OrderId>>
                    GROUP BY <<o>>.<<CustomerId>>, <<o>>.<<Id>>
                ) AS <<stats>>
                WHERE <<stats>>.<<CustomerId>> = !!100
                ORDER BY <<stats>>.<<TotalAmount>> DESC
                LIMIT !!101 OFFSET !!102
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT "stats"."OrderId", "stats"."TotalAmount"
                FROM (
                    SELECT 
                        "o"."CustomerId",
                        "o"."Id" AS "OrderId", 
                        SUM("ol"."Price") AS "TotalAmount"
                    FROM "dbo"."Orders" AS "o"
                    JOIN "OrderLine" AS "ol" ON "o"."Id" = "ol"."OrderId"
                    GROUP BY "o"."CustomerId", "o"."Id"
                ) AS "stats"
                WHERE "stats"."CustomerId" = @p0
                ORDER BY "stats"."TotalAmount" DESC
                FIRST @p1 SKIP @p2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `stats`.`OrderId`, `stats`.`TotalAmount`
                FROM (
                    SELECT 
                        `o`.`CustomerId`,
                        `o`.`Id` AS `OrderId`, 
                        SUM(`ol`.`Price`) AS `TotalAmount`
                    FROM `dbo`.`Orders` AS `o`
                    JOIN `OrderLine` AS `ol` ON `o`.`Id` = `ol`.`OrderId`
                    GROUP BY `o`.`CustomerId`, `o`.`Id`
                ) AS `stats`
                WHERE `stats`.`CustomerId` = @p0
                ORDER BY `stats`.`TotalAmount` DESC
                LIMIT @p1 OFFSET @p2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "stats"."OrderId", "stats"."TotalAmount"
                FROM (
                    SELECT 
                        "o"."CustomerId",
                        "o"."Id" AS "OrderId", 
                        SUM("ol"."Price") AS "TotalAmount"
                    FROM "dbo"."Orders" "o"
                    JOIN "OrderLine" "ol" ON "o"."Id" = "ol"."OrderId"
                    GROUP BY "o"."CustomerId", "o"."Id"
                ) "stats"
                WHERE "stats"."CustomerId" = :0
                ORDER BY "stats"."TotalAmount" DESC
                OFFSET :2 ROWS FETCH NEXT :1 ROWS ONLY
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "stats"."OrderId", "stats"."TotalAmount"
                FROM (
                    SELECT 
                        "o"."CustomerId",
                        "o"."Id" AS "OrderId", 
                        SUM("ol"."Price") AS "TotalAmount"
                    FROM "dbo"."Orders" AS "o"
                    JOIN "OrderLine" AS "ol" ON "o"."Id" = "ol"."OrderId"
                    GROUP BY "o"."CustomerId", "o"."Id"
                ) AS "stats"
                WHERE "stats"."CustomerId" = $1
                ORDER BY "stats"."TotalAmount" DESC
                LIMIT $2 OFFSET $3
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "stats"."OrderId", "stats"."TotalAmount"
                FROM (
                    SELECT 
                        "o"."CustomerId",
                        "o"."Id" AS "OrderId", 
                        SUM("ol"."Price") AS "TotalAmount"
                    FROM "dbo"."Orders" AS "o"
                    JOIN "OrderLine" AS "ol" ON "o"."Id" = "ol"."OrderId"
                    GROUP BY "o"."CustomerId", "o"."Id"
                ) AS "stats"
                WHERE "stats"."CustomerId" = @p1
                ORDER BY "stats"."TotalAmount" DESC
                LIMIT @p2 OFFSET @p3
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [stats].[OrderId], [stats].[TotalAmount]
                FROM (
                    SELECT 
                        [o].[CustomerId],
                        [o].[Id] AS [OrderId], 
                        SUM([ol].[Price]) AS [TotalAmount]
                    FROM [dbo].[Orders] AS [o]
                    JOIN [OrderLine] AS [ol] ON [o].[Id] = [ol].[OrderId]
                    GROUP BY [o].[CustomerId], [o].[Id]
                ) AS [stats]
                WHERE [stats].[CustomerId] = @p0
                ORDER BY [stats].[TotalAmount] DESC
                OFFSET @p2 ROWS FETCH NEXT @p1 ROWS ONLY
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> AdvancedDynamicQueryData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<stats>>.<<OrderId>>, <<stats>>.<<ProductName>>, <<stats>>.<<TotalAmount>>
                FROM (
                    SELECT 
                        <<o>>.<<Id>> AS <<OrderId>>, 
                        <<p>>.<<PROD_NAME>> AS <<ProductName>>,
                        <<ol_agg>>.<<TotalAmount>> AS <<TotalAmount>>
                    FROM <<dbo>>.<<Orders>> AS <<o>>
                    
                    JOIN (
                        SELECT 
                            <<ol>>.<<OrderId>> AS <<OrderId>>,
                            <<ol>>.<<ProductId>> AS <<ProductId>>,
                            SUM(<<ol>>.<<Price>>) AS <<TotalAmount>>
                        -- JOIN ol_agg ON ...
                        FROM <<OrderLine>> AS <<ol>>
                        GROUP BY <<ol>>.<<OrderId>>, <<ol>>.<<ProductId>>
                    ) AS <<ol_agg>> ON <<o>>.<<Id>> = <<ol_agg>>.<<OrderId>>
                    
                    JOIN <<dbo>>.<<Products>> AS <<p>> ON <<ol_agg>>.<<ProductId>> = <<p>>.<<Id>>
                    JOIN <<Category>> AS <<cat>> ON <<p>>.<<CategoryId>> = <<cat>>.<<Id>>
                ) AS <<stats>>
                WHERE <<stats>>.<<ProductName>> = !!100
                ORDER BY <<stats>>.<<TotalAmount>> DESC
                LIMIT !!101 OFFSET !!102
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT "stats"."OrderId", "stats"."ProductName", "stats"."TotalAmount"
                FROM (
                    SELECT 
                        "o"."Id" AS "OrderId", 
                        "p"."PROD_NAME" AS "ProductName",
                        "ol_agg"."TotalAmount" AS "TotalAmount"
                    FROM "dbo"."Orders" AS "o"
                    
                    JOIN (
                        SELECT 
                            "ol"."OrderId" AS "OrderId",
                            "ol"."ProductId" AS "ProductId",
                            SUM("ol"."Price") AS "TotalAmount"
                        -- JOIN ol_agg ON ...
                        FROM "OrderLine" AS "ol"
                        GROUP BY "ol"."OrderId", "ol"."ProductId"
                    ) AS "ol_agg" ON "o"."Id" = "ol_agg"."OrderId"
                    
                    JOIN "dbo"."Products" AS "p" ON "ol_agg"."ProductId" = "p"."Id"
                    JOIN "Category" AS "cat" ON "p"."CategoryId" = "cat"."Id"
                ) AS "stats"
                WHERE "stats"."ProductName" = @p0
                ORDER BY "stats"."TotalAmount" DESC
                FIRST @p1 SKIP @p2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `stats`.`OrderId`, `stats`.`ProductName`, `stats`.`TotalAmount`
                FROM (
                    SELECT 
                        `o`.`Id` AS `OrderId`, 
                        `p`.`PROD_NAME` AS `ProductName`,
                        `ol_agg`.`TotalAmount` AS `TotalAmount`
                    FROM `dbo`.`Orders` AS `o`
                    
                    JOIN (
                        SELECT 
                            `ol`.`OrderId` AS `OrderId`,
                            `ol`.`ProductId` AS `ProductId`,
                            SUM(`ol`.`Price`) AS `TotalAmount`
                        -- JOIN ol_agg ON ...
                        FROM `OrderLine` AS `ol`
                        GROUP BY `ol`.`OrderId`, `ol`.`ProductId`
                    ) AS `ol_agg` ON `o`.`Id` = `ol_agg`.`OrderId`
                    
                    JOIN `dbo`.`Products` AS `p` ON `ol_agg`.`ProductId` = `p`.`Id`
                    JOIN `Category` AS `cat` ON `p`.`CategoryId` = `cat`.`Id`
                ) AS `stats`
                WHERE `stats`.`ProductName` = @p0
                ORDER BY `stats`.`TotalAmount` DESC
                LIMIT @p1 OFFSET @p2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "stats"."OrderId", "stats"."ProductName", "stats"."TotalAmount"
                FROM (
                    SELECT 
                        "o"."Id" AS "OrderId", 
                        "p"."PROD_NAME" AS "ProductName",
                        "ol_agg"."TotalAmount" AS "TotalAmount"
                    FROM "dbo"."Orders" "o"
                    
                    JOIN (
                        SELECT 
                            "ol"."OrderId" AS "OrderId",
                            "ol"."ProductId" AS "ProductId",
                            SUM("ol"."Price") AS "TotalAmount"
                        -- JOIN ol_agg ON ...
                        FROM "OrderLine" "ol"
                        GROUP BY "ol"."OrderId", "ol"."ProductId"
                    ) "ol_agg" ON "o"."Id" = "ol_agg"."OrderId"
                    
                    JOIN "dbo"."Products" "p" ON "ol_agg"."ProductId" = "p"."Id"
                    JOIN "Category" "cat" ON "p"."CategoryId" = "cat"."Id"
                ) "stats"
                WHERE "stats"."ProductName" = :0
                ORDER BY "stats"."TotalAmount" DESC
                OFFSET :2 ROWS FETCH NEXT :1 ROWS ONLY
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "stats"."OrderId", "stats"."ProductName", "stats"."TotalAmount"
                FROM (
                    SELECT 
                        "o"."Id" AS "OrderId", 
                        "p"."PROD_NAME" AS "ProductName",
                        "ol_agg"."TotalAmount" AS "TotalAmount"
                    FROM "dbo"."Orders" AS "o"
                    
                    JOIN (
                        SELECT 
                            "ol"."OrderId" AS "OrderId",
                            "ol"."ProductId" AS "ProductId",
                            SUM("ol"."Price") AS "TotalAmount"
                        -- JOIN ol_agg ON ...
                        FROM "OrderLine" AS "ol"
                        GROUP BY "ol"."OrderId", "ol"."ProductId"
                    ) AS "ol_agg" ON "o"."Id" = "ol_agg"."OrderId"
                    
                    JOIN "dbo"."Products" AS "p" ON "ol_agg"."ProductId" = "p"."Id"
                    JOIN "Category" AS "cat" ON "p"."CategoryId" = "cat"."Id"
                ) AS "stats"
                WHERE "stats"."ProductName" = $1
                ORDER BY "stats"."TotalAmount" DESC
                LIMIT $2 OFFSET $3
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "stats"."OrderId", "stats"."ProductName", "stats"."TotalAmount"
                FROM (
                    SELECT 
                        "o"."Id" AS "OrderId", 
                        "p"."PROD_NAME" AS "ProductName",
                        "ol_agg"."TotalAmount" AS "TotalAmount"
                    FROM "dbo"."Orders" AS "o"
                    
                    JOIN (
                        SELECT 
                            "ol"."OrderId" AS "OrderId",
                            "ol"."ProductId" AS "ProductId",
                            SUM("ol"."Price") AS "TotalAmount"
                        -- JOIN ol_agg ON ...
                        FROM "OrderLine" AS "ol"
                        GROUP BY "ol"."OrderId", "ol"."ProductId"
                    ) AS "ol_agg" ON "o"."Id" = "ol_agg"."OrderId"
                    
                    JOIN "dbo"."Products" AS "p" ON "ol_agg"."ProductId" = "p"."Id"
                    JOIN "Category" AS "cat" ON "p"."CategoryId" = "cat"."Id"
                ) AS "stats"
                WHERE "stats"."ProductName" = @p1
                ORDER BY "stats"."TotalAmount" DESC
                LIMIT @p2 OFFSET @p3
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [stats].[OrderId], [stats].[ProductName], [stats].[TotalAmount]
                FROM (
                    SELECT 
                        [o].[Id] AS [OrderId], 
                        [p].[PROD_NAME] AS [ProductName],
                        [ol_agg].[TotalAmount] AS [TotalAmount]
                    FROM [dbo].[Orders] AS [o]
                    
                    JOIN (
                        SELECT 
                            [ol].[OrderId] AS [OrderId],
                            [ol].[ProductId] AS [ProductId],
                            SUM([ol].[Price]) AS [TotalAmount]
                        -- JOIN ol_agg ON ...
                        FROM [OrderLine] AS [ol]
                        GROUP BY [ol].[OrderId], [ol].[ProductId]
                    ) AS [ol_agg] ON [o].[Id] = [ol_agg].[OrderId]
                    
                    JOIN [dbo].[Products] AS [p] ON [ol_agg].[ProductId] = [p].[Id]
                    JOIN [Category] AS [cat] ON [p].[CategoryId] = [cat].[Id]
                ) AS [stats]
                WHERE [stats].[ProductName] = @p0
                ORDER BY [stats].[TotalAmount] DESC
                OFFSET @p2 ROWS FETCH NEXT @p1 ROWS ONLY
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> ComplexRawSqlData =>
    [
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Price" > @p0
                  AND p.Status = 'ACTIVE' /* Raw SQL condition */
                GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                HAVING COUNT(*) > 1
                ORDER BY "dbo"."Products"."PROD_NAME" DESC
                LIMIT 10 OFFSET 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`Price` > @p0
                  AND p.Status = 'ACTIVE' /* Raw SQL condition */
                GROUP BY `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                HAVING COUNT(*) > 1
                ORDER BY `dbo`.`Products`.`PROD_NAME` DESC
                LIMIT 10 OFFSET 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Price" > :0
                  AND p.Status = 'ACTIVE' /* Raw SQL condition */
                GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                HAVING COUNT(*) > 1
                ORDER BY "dbo"."Products"."PROD_NAME" DESC
                OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Price" > $1
                  AND p.Status = 'ACTIVE' /* Raw SQL condition */
                GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                HAVING COUNT(*) > 1
                ORDER BY "dbo"."Products"."PROD_NAME" DESC
                LIMIT 10 OFFSET 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Price" > @p1
                  AND p.Status = 'ACTIVE' /* Raw SQL condition */
                GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                HAVING COUNT(*) > 1
                ORDER BY "dbo"."Products"."PROD_NAME" DESC
                LIMIT 10 OFFSET 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products]
                WHERE [dbo].[Products].[Price] > @p0
                  AND p.Status = 'ACTIVE' /* Raw SQL condition */
                GROUP BY [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                HAVING COUNT(*) > 1
                ORDER BY [dbo].[Products].[PROD_NAME] DESC
                OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY
                """
            ]
        )
    ];
}