using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IAdvancedTestSuite))]
public abstract partial class AdvancedTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder();

    [SqlTest(nameof(IAdvancedTestSuite.DynamicQueryData))]
    public void DynamicQuery(SqlTestCase testCase)
    {
        // Arrange
        var request = new GetOrderStatsRequest
        {
            CustomerId = 5,
            SelectFields = ["OrderId", "TotalAmount"],
            SortFields = [new SortCriteria("TotalAmount", true)],
            Page = 2,
            PageSize = 20
        };
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<ApiOrderStatsModel>(out var stats, "stats")
              .Entity<OrderModel>(out var o, "o")
              .Entity<OrderLine>(out var ol, "ol");

#pragma warning disable SQLIG10 // Highly dynamic query parts gracefully fallback to JIT
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
#pragma warning restore SQLIG10

            return db.Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted(); 
    }

    [SqlTest(nameof(IAdvancedTestSuite.AdvancedDynamicQueryData))]
    public void AdvancedDynamicQuery(SqlTestCase testCase)
    {
        // Arrange
        var request = new GetMassiveStatsRequest
        {
            ProductNameFilter = "Laptop",
            SelectFields = ["OrderId", "ProductName", "TotalAmount"],
            SortFields = [new SortCriteria("TotalAmount", true)], 
            Page = 2, 
            PageSize = 20
        };
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<MassiveOrderStatsModel>(out var stats, "stats")
              .Entity<OrderModel>(out var o, "o")
              .Entity<OrderLine>(out var ol, "ol")
              .Entity<Product>(out var p, "p")
              .Entity<Category>(out var cat, "cat")
              .Entity<OrderLineAggModel>(out var olAgg, "ol_agg");

#pragma warning disable SQLIG10
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
#pragma warning restore SQLIG10

            return db.Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IAdvancedTestSuite.ComplexRawSqlData))]
    public void RawSql_ComplexStatements_PassThroughUnmodified(SqlTestCase testCase)
    {
        // Arrange
        var minPrice = 50.00m;
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
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

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}