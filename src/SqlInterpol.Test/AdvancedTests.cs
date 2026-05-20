using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class AdvancedTests
{
    [Theory]
    [MemberData(nameof(DynamicQueryData))]
    public void DynamicQuery(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        var request = new GetOrderStatsRequest
        {
            CustomerId = 5,
            SelectFields = ["OrderId", "TotalAmount"],
            SortFields = [new SortCriteria("TotalAmount", true)],
            Page = 2,
            PageSize = 20
        };

        var result = db
            .Entity<ApiOrderStatsModel>()
            .Entity<OrderModel>(alias: "o")
            .Entity<OrderLine>(alias: "ol")
            .Query((stats, o, ol) =>
            {
                var dynamicSelects = request.SelectFields.Select(f => stats[f]);
                var dynamicSorts = request.SortFields.Select(sort => 
                    stats.OrderBy(sort.Field, sort.Descending ? SqlOrderDirection.Desc : SqlOrderDirection.Asc));

                db.AppendLine($$"""
                    SELECT {{dynamicSelects}}
                    FROM (
                        SELECT 
                            {{o[x => x.CustomerId]}},
                            {{o[x => x.Id]}} AS {{ol[x => x.OrderId]}}, 
                            SUM({{ol[x => x.Price]}}) AS {{stats[x => x.TotalAmount]}}
                        FROM {{o}}
                        JOIN {{ol}} ON {{o[x => x.Id]}} = {{ol[x => x.OrderId]}}
                        GROUP BY {{o[x => x.CustomerId]}}, {{o[x => x.Id]}}
                    ) AS {{stats.As("stats")}}
                    """);

                if (request.CustomerId.HasValue) 
                    db.AppendLine($"WHERE {stats[x => x.CustomerId]} = {request.CustomerId}"); 
                
                if (dynamicSorts.Any()) 
                    db.AppendLine($"ORDER BY {dynamicSorts}");

                int offset = (request.Page - 1) * request.PageSize;
                db.Append($"LIMIT {request.PageSize} OFFSET {offset}");
            }).Build();

        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(AdvancedDynamicQueryData))]
    public void AdvancedDynamicQuery(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        var request = new GetMassiveStatsRequest
        {
            ProductNameFilter = "Laptop",
            SelectFields = ["OrderId", "ProductName", "TotalAmount"],
            SortFields = [new SortCriteria("TotalAmount", true)], 
            Page = 2, 
            PageSize = 20
        };

        var o = db.AddEntity<OrderModel>(alias: "o");
        var ol = db.AddEntity<OrderLine>(alias: "ol");
        var p = db.AddEntity<Product>(alias: "p");
        var cat = db.AddEntity<Category>(alias: "cat");
        
        var olAgg = db.AddEntity<OrderLineAggModel>(alias: "ol_agg");

        var result = db.Query<MassiveOrderStatsModel>(stats =>
        {
            var dynamicSelects = request.SelectFields.Select(f => stats[f]);
            var dynamicSorts = request.SortFields.Select(sort => 
                stats.OrderBy(sort.Field, sort.Descending ? SqlOrderDirection.Desc : SqlOrderDirection.Asc));

            db.AppendLine($$"""
                SELECT {{dynamicSelects}}
                FROM (
                    SELECT 
                        {{o[x => x.Id]}} AS {{stats[x => x.OrderId]}}, 
                        {{p[x => x.Name]}} AS {{stats[x => x.ProductName]}},
                        {{olAgg[x => x.TotalAmount]}} AS {{stats[x => x.TotalAmount]}}
                    FROM {{o}}
                    
                    JOIN (
                        SELECT 
                            {{ol[x => x.OrderId]}} AS {{olAgg[x => x.OrderId]}},
                            {{ol[x => x.ProductId]}} AS {{olAgg[x => x.ProductId]}},
                            SUM({{ol[x => x.Price]}}) AS {{olAgg[x => x.TotalAmount]}}
                        -- JOIN {{"ol_agg"}} ON ...
                        FROM {{ol}}
                        GROUP BY {{ol[x => x.OrderId]}}, {{ol[x => x.ProductId]}}
                    ) AS {{olAgg.As("ol_agg")}} ON {{o[x => x.Id]}} = {{olAgg[x => x.OrderId]}}
                    
                    JOIN {{p}} ON {{olAgg[x => x.ProductId]}} = {{p[x => x.Id]}}
                    JOIN {{cat}} ON {{p[x => x.CategoryId]}} = {{cat[x => x.Id]}}
                ) AS {{stats.As("stats")}}
                """);

            if (!string.IsNullOrEmpty(request.ProductNameFilter)) 
                db.AppendLine($"WHERE {stats[x => x.ProductName]} = {request.ProductNameFilter}"); 
            
            if (dynamicSorts.Any()) 
                db.AppendLine($"ORDER BY {dynamicSorts}");

            int offset = (request.Page - 1) * request.PageSize;
            db.Append($"LIMIT {request.PageSize} OFFSET {offset}");
        }).Build();

        testCase.AssertSql(result.Sql);
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
                    FROM "dbo"."Orders" AS "o"
                    JOIN "OrderLine" AS "ol" ON "o"."Id" = "ol"."OrderId"
                    GROUP BY "o"."CustomerId", "o"."Id"
                ) AS "stats"
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
                WHERE "stats"."CustomerId" = ?0
                ORDER BY "stats"."TotalAmount" DESC
                LIMIT ?1 OFFSET ?2
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
                        -- JOIN !!100 ON ...
                        FROM <<OrderLine>> AS <<ol>>
                        GROUP BY <<ol>>.<<OrderId>>, <<ol>>.<<ProductId>>
                    ) AS <<ol_agg>> ON <<o>>.<<Id>> = <<ol_agg>>.<<OrderId>>
                    
                    JOIN <<dbo>>.<<Products>> AS <<p>> ON <<ol_agg>>.<<ProductId>> = <<p>>.<<Id>>
                    JOIN <<Category>> AS <<cat>> ON <<p>>.<<CategoryId>> = <<cat>>.<<Id>>
                ) AS <<stats>>
                WHERE <<stats>>.<<ProductName>> = !!101
                ORDER BY <<stats>>.<<TotalAmount>> DESC
                LIMIT !!102 OFFSET !!103
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
                        -- JOIN @p0 ON ...
                        FROM "OrderLine" AS "ol"
                        GROUP BY "ol"."OrderId", "ol"."ProductId"
                    ) AS "ol_agg" ON "o"."Id" = "ol_agg"."OrderId"
                    
                    JOIN "dbo"."Products" AS "p" ON "ol_agg"."ProductId" = "p"."Id"
                    JOIN "Category" AS "cat" ON "p"."CategoryId" = "cat"."Id"
                ) AS "stats"
                WHERE "stats"."ProductName" = @p1
                ORDER BY "stats"."TotalAmount" DESC
                FIRST @p2 SKIP @p3
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
                        -- JOIN @p0 ON ...
                        FROM `OrderLine` AS `ol`
                        GROUP BY `ol`.`OrderId`, `ol`.`ProductId`
                    ) AS `ol_agg` ON `o`.`Id` = `ol_agg`.`OrderId`
                    
                    JOIN `dbo`.`Products` AS `p` ON `ol_agg`.`ProductId` = `p`.`Id`
                    JOIN `Category` AS `cat` ON `p`.`CategoryId` = `cat`.`Id`
                ) AS `stats`
                WHERE `stats`.`ProductName` = @p1
                ORDER BY `stats`.`TotalAmount` DESC
                LIMIT @p2 OFFSET @p3
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
                    FROM "dbo"."Orders" AS "o"
                    
                    JOIN (
                        SELECT 
                            "ol"."OrderId" AS "OrderId",
                            "ol"."ProductId" AS "ProductId",
                            SUM("ol"."Price") AS "TotalAmount"
                        -- JOIN :0 ON ...
                        FROM "OrderLine" AS "ol"
                        GROUP BY "ol"."OrderId", "ol"."ProductId"
                    ) AS "ol_agg" ON "o"."Id" = "ol_agg"."OrderId"
                    
                    JOIN "dbo"."Products" AS "p" ON "ol_agg"."ProductId" = "p"."Id"
                    JOIN "Category" AS "cat" ON "p"."CategoryId" = "cat"."Id"
                ) AS "stats"
                WHERE "stats"."ProductName" = :1
                ORDER BY "stats"."TotalAmount" DESC
                OFFSET :3 ROWS FETCH NEXT :2 ROWS ONLY
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
                        -- JOIN $1 ON ...
                        FROM "OrderLine" AS "ol"
                        GROUP BY "ol"."OrderId", "ol"."ProductId"
                    ) AS "ol_agg" ON "o"."Id" = "ol_agg"."OrderId"
                    
                    JOIN "dbo"."Products" AS "p" ON "ol_agg"."ProductId" = "p"."Id"
                    JOIN "Category" AS "cat" ON "p"."CategoryId" = "cat"."Id"
                ) AS "stats"
                WHERE "stats"."ProductName" = $2
                ORDER BY "stats"."TotalAmount" DESC
                LIMIT $3 OFFSET $4
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
                        -- JOIN ?0 ON ...
                        FROM "OrderLine" AS "ol"
                        GROUP BY "ol"."OrderId", "ol"."ProductId"
                    ) AS "ol_agg" ON "o"."Id" = "ol_agg"."OrderId"
                    
                    JOIN "dbo"."Products" AS "p" ON "ol_agg"."ProductId" = "p"."Id"
                    JOIN "Category" AS "cat" ON "p"."CategoryId" = "cat"."Id"
                ) AS "stats"
                WHERE "stats"."ProductName" = ?1
                ORDER BY "stats"."TotalAmount" DESC
                LIMIT ?2 OFFSET ?3
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
                        -- JOIN @p0 ON ...
                        FROM [OrderLine] AS [ol]
                        GROUP BY [ol].[OrderId], [ol].[ProductId]
                    ) AS [ol_agg] ON [o].[Id] = [ol_agg].[OrderId]
                    
                    JOIN [dbo].[Products] AS [p] ON [ol_agg].[ProductId] = [p].[Id]
                    JOIN [Category] AS [cat] ON [p].[CategoryId] = [cat].[Id]
                ) AS [stats]
                WHERE [stats].[ProductName] = @p1
                ORDER BY [stats].[TotalAmount] DESC
                OFFSET @p3 ROWS FETCH NEXT @p2 ROWS ONLY
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(ComplexRawSqlData))]
    public void RawSql_ComplexStatements_PassThroughUnmodified(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var minPrice = 50.00m;

        // Act
        // We are mixing AST nodes {{p}}, parameters {{minPrice}}, and RAW SQL here!
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
            FROM {{p}}
            WHERE {{p[x => x.Price]}} > {{minPrice}}
              AND p.Status = 'ACTIVE' /* Raw SQL condition */
            GROUP BY {{p[x => x.Id]}}, {{p[x => x.Name]}}
            HAVING COUNT(*) > 1
            ORDER BY {{p[x => x.Name]}} DESC
            LIMIT 10 OFFSET 5
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Single(result.Parameters);
        Assert.Equal(minPrice, result.Parameters.First().Value);
    }

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
                LIMIT 10 OFFSET 5
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
                WHERE "dbo"."Products"."Price" > ?0
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
                LIMIT 10 OFFSET 5
                """
            ]
        )
    ];
}