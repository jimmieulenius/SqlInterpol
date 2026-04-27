using SqlInterpol.Enums;
using SqlInterpol.Models;
using SqlInterpol.Test.Models;
using SqlSchema;

namespace SqlInterpol.Test;

public class SqlInterpolTests
{
    [Fact]
    public void SimpleTableAlias_ShouldRenderWithCustomAlias()
    {
        // Arrange
        // Note: This assumes Product is defined with [SqlTable(schemaName: "dbo")] 
        // and maps to the "Product" table.
        var p = Sql.GetTable<Product>();
        
        // We expect the table name to be quoted based on the default (SQL Server) 
        // and the alias to be applied correctly.
        var expectedSql = "FROM [dbo].[Product] AS [t]";

        // Act
        var query = Sql.Build($"FROM {p} AS {p.Alias("t")}");

        // Assert
        // Using Trim() to ignore leading/trailing whitespace, 
        // but Equal() ensures the exact SQL structure is maintained.
        Assert.Equal(expectedSql, query.Sql);
    }

    [Fact]
    public void NaturalAsAlias_ShouldCaptureAliasFromLiteralString()
    {
        // Arrange
        var p = Sql.GetTable<Product>();
        var expectedSql = "SELECT [p].[ItemNumber] FROM [dbo].[Product] AS [p]";

        // Act 
        // Here we use the natural SQL 'AS' keyword instead of calling p.As("p")
        var query = Sql.Build($"SELECT {p[x => x.ItemNumber]} FROM {p} AS p");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
        
        // Crucial: Verify the table object itself now "knows" its alias is 'p'
        // This is what allows subsequent columns to use [p].[Column]
        Assert.Equal("p", p.Alias()); 
    }

    [Fact]
    public void NaturalAlias_ShouldBeDialectAware()
    {
        // Arrange
        var p = Sql.GetTable<Product>();
        var dbType = SqlDialect.PostgreSql;
        // Postgres should quote the alias with double quotes
        var expectedSql = "SELECT \"p\".\"ItemNumber\" FROM \"dbo\".\"Product\" AS \"p\"";

        // Act
        var query = Sql.Build(dbType, $"SELECT {p[x => x.ItemNumber]} FROM {p} AS p");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
    }

    [Fact]
    public void StringParameter_ShouldBeParametrizedAndStored()
    {
        // Arrange
        var userName = "John Doe";
        var expectedSql = "SELECT * FROM users WHERE name = @p0";

        // Act
        var query = Sql.Build($"SELECT * FROM users WHERE name = {userName}");

        // Assert
        // Verify the SQL structure
        Assert.Equal(expectedSql, query.Sql);
        
        // Verify the parameter count and value
        Assert.Single(query.Parameters);
        Assert.True(query.Parameters.ContainsKey("@p0"), "Parameters should contain the key '@p0'");
        Assert.Equal(userName, query.Parameters["@p0"]);
    }

    [Fact]
    public void BracketStringParameter_ShouldBeParametrizedAndNotTreatedAsIdentifier()
    {
        // Arrange
        var productCode = "[TEST123]";
        var expectedSql = "WHERE code = @p0";

        // Act
        var query = Sql.Build($"WHERE code = {productCode}");

        // Assert
        // Verify that the brackets didn't trigger 'raw' string handling
        Assert.Equal(expectedSql, query.Sql);
        
        // Verify the parameter value remains intact
        Assert.Single(query.Parameters);
        Assert.Equal(productCode, query.Parameters["@p0"]);
    }

    [Fact]
    public void ConditionalClausesAndFormat_ShouldRenderCorrectLayoutAndParams()
    {
        // Arrange
        var productsTable = Dbo.Products;
        var minPrice = 100;
        var shouldAddOrderBy = true;
        var shouldAddWhere = true;

        // We use a verbatim string to match the exact whitespace/newlines 
        // expected from your console output.
        var expectedSql = @"
SELECT
    [p].[ItemNumber],
    [p].[ProductName] AS [Product],
    [p].[Price]
FROM [dbo].[ReleasedProducts] AS [p]
WHERE [p].[Price] > @p0
ORDER BY [Product] ASC".Trim();

        // Act
        var query = Sql.Build($@"SELECT
    {productsTable.ItemNumber},
    {productsTable.ProductName.As("Product")},
    {productsTable.Price}
FROM {productsTable.As("p")}
{(shouldAddWhere ? Sql.Format("WHERE {0} > {1}", productsTable.Price, minPrice) : null)}
{(shouldAddOrderBy ? Sql.Format("ORDER BY {0} ASC", productsTable.ProductName) : null)}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
        Assert.Single(query.Parameters);
        Assert.Equal(100, query.Parameters["@p0"]);
    }

    [Fact]
    public void AttributeBasedQuery_ShouldResolveFromPocoAttributes()
    {
        // Arrange
        var minPrice = 100;
        var expectedSql = @"
SELECT 
    [dbo].[Product].[ItemNumber],
    [dbo].[Product].[Name],
    [dbo].[Product].[Price]
FROM [dbo].[Product]
WHERE [dbo].[Product].[Price] > @p0".Trim();

        // Act
        var attributeQuery = Sql.GetTable<Product>().Query(table => Sql.Build($@"SELECT 
    {table[p => p.ItemNumber]},
    {table[p => p.Name]},
    {table[p => p.Price]}
FROM {table}
WHERE {table[p => p.Price]} > {minPrice}"));

        // Assert
        Assert.Equal(expectedSql, attributeQuery.Sql);
        Assert.Single(attributeQuery.Parameters);
        Assert.Equal(100, attributeQuery.Parameters["@p0"]);
    }

    [Fact]
    public void InsertQuery_ShouldRenderValuesCorrectly()
    {
        // Arrange
        var productsTable = Dbo.Products;
        var newItemNumber = 99999;
        var newProductName = "New Widget Pro";
        var newPrice = 149.99m;

        // Verbatim string to match your exact console output layout
        var expectedSql = @"
INSERT INTO [dbo].[ReleasedProducts]
([ItemNumber], [ProductName], [Price])
VALUES (@p0, @p1, @p2)".Trim();

        // Act
        var query = Sql.Build($@"INSERT INTO {productsTable}
({productsTable.ItemNumber}, {productsTable.ProductName}, {productsTable.Price})
{Sql.Format("VALUES ({0}, {1}, {2})", newItemNumber, newProductName, newPrice)}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
        Assert.Equal(3, query.Parameters.Count);
        Assert.Equal(newItemNumber, query.Parameters["@p0"]);
        Assert.Equal(newProductName, query.Parameters["@p1"]);
        Assert.Equal(newPrice, query.Parameters["@p2"]);
    }

    [Fact]
    public void UpdateQuery_ShouldDifferentiateBetweenSetAndWhereClauses()
    {
        // Arrange
        var productsTable = Dbo.Products;
        var newProductName = "New Widget Pro";
        var updatePrice = 159.99m;
        var updateItemNumber = 99999;

        var expectedSql = @"
UPDATE [dbo].[ReleasedProducts]
SET [ProductName] = @p0, [Price] = @p1
WHERE [dbo].[ReleasedProducts].[ItemNumber] = @p2".Trim();

        // Act
        var query = Sql.Build($@"UPDATE {productsTable}
SET {productsTable.ProductName} = {newProductName}, {productsTable.Price} = {updatePrice}
WHERE {productsTable.ItemNumber} = {updateItemNumber}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
        Assert.Equal(3, query.Parameters.Count);
        Assert.Equal(newProductName, query.Parameters["@p0"]);
        Assert.Equal(updatePrice, query.Parameters["@p1"]);
        Assert.Equal(updateItemNumber, query.Parameters["@p2"]);
    }

    [Fact]
    public void DeleteQuery_ShouldRenderCorrectTargetAndCriteria()
    {
        // Arrange
        var productsTable = Dbo.Products;
        var updateItemNumber = 99999;
        var expectedSql = @"
DELETE FROM [dbo].[ReleasedProducts]
WHERE [dbo].[ReleasedProducts].[ItemNumber] = @p0".Trim();

        // Act
        var query = Sql.Build($@"DELETE FROM {productsTable}
WHERE {productsTable.ItemNumber} = {updateItemNumber}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
        Assert.Single(query.Parameters);
        Assert.Equal(updateItemNumber, query.Parameters["@p0"]);
    }

    [Fact]
    public void InnerJoin_ShouldRenderCorrectJoinAndOnClauses()
    {
        // Arrange
        var prodTable = Sql.GetTable<Product>();
        var orderTable = Sql.GetTable<Order>();
        var lastMonth = DateTime.Now.AddMonths(-1);

        // Act
        var query = Sql.Build($@"SELECT 
  {prodTable[p => p.ItemNumber]},
  {prodTable[p => p.Name]},
  {orderTable[o => o.OrderId]},
  {orderTable[o => o.OrderDate]}
FROM {prodTable.InnerJoin(orderTable).On(prodTable[p => p.ItemNumber], orderTable[o => o.ProductItemNumber])}
WHERE {orderTable[o => o.OrderDate]} > {lastMonth}");

        // Assert
        var expectedSql = $@"
SELECT 
  [dbo].[Product].[ItemNumber],
  [dbo].[Product].[Name],
  [dbo].[Orders].[OrderId],
  [dbo].[Orders].[OrderDate]
FROM [dbo].[Product]
INNER JOIN [dbo].[Orders]
  ON [dbo].[Product].[ItemNumber] = [dbo].[Orders].[ProductItemNumber]
WHERE [dbo].[Orders].[OrderDate] > @p0".Trim();

        Assert.Equal(expectedSql, query.Sql);
        Assert.Equal(lastMonth, query.Parameters["@p0"]);
    }

    [Fact]
    public void MultipleJoins_ShouldChainCorrectlyInOrder()
    {
        // Arrange
        var prodTable = Sql.GetTable<Product>();
        var orderTable = Sql.GetTable<Order>();

        // Act
        var query = Sql.Build($@"SELECT 
  {prodTable[p => p.ItemNumber]},
  {prodTable[p => p.Name]},
  {orderTable[o => o.OrderId]}
FROM {prodTable
    .InnerJoin(orderTable).On(prodTable[p => p.ItemNumber], orderTable[o => o.ProductItemNumber])
    .LeftJoin(orderTable).On(prodTable[p => p.ItemNumber], orderTable[o => o.ProductItemNumber])
}");

        // Assert
        var expectedSql = @"
SELECT 
  [dbo].[Product].[ItemNumber],
  [dbo].[Product].[Name],
  [dbo].[Orders].[OrderId]
FROM [dbo].[Product]
INNER JOIN [dbo].[Orders]
  ON [dbo].[Product].[ItemNumber] = [dbo].[Orders].[ProductItemNumber]
LEFT JOIN [dbo].[Orders]
  ON [dbo].[Product].[ItemNumber] = [dbo].[Orders].[ProductItemNumber]".Trim();

        Assert.Equal(expectedSql, query.Sql);
    }

    [Fact]
    public void CrossJoin_ShouldRenderWithoutOnClause()
    {
        // Arrange
        var prodTable = Sql.GetTable<Product>();
        var orderTable = Sql.GetTable<Order>();
        var expectedSql = @"
SELECT TOP 10
  [dbo].[Product].[ItemNumber],
  [dbo].[Orders].[OrderId]
FROM [dbo].[Product]
CROSS JOIN [dbo].[Orders]".Trim();

        // Act
        var query = Sql.Build($@"SELECT TOP 10
  {prodTable[p => p.ItemNumber]},
  {orderTable[o => o.OrderId]}
FROM {prodTable.CrossJoin(orderTable)}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
    }

    [Fact]
    public void JoinWithAliases_ShouldRenderColumnsDifferentlyBasedOnClause()
    {
        // Arrange
        var p_join = Sql.GetTable<Product>();
        var o = Sql.GetTable<Order>();
        var orderQuantity = 10;
        var expectedSql = @"
SELECT
  [p].[ItemNumber] AS [SKU],
  [p].[Name],
  [o].[OrderId] AS [OrderNumber],
  [o].[OrderDate] AS [OrderedDate]
FROM [dbo].[Product] AS [p]
INNER JOIN [dbo].[Orders] AS [o]
  ON [p].[ItemNumber] = [o].[ProductItemNumber]
WHERE [o].[Quantity] > @p0
ORDER BY [OrderedDate] DESC".Trim();

        // Act
        var query = Sql.Build($@"SELECT
  {p_join[prod => prod.ItemNumber].As("SKU")},
  {p_join[prod => prod.Name]},
  {o[ord => ord.OrderId].As("OrderNumber")},
  {o[ord => ord.OrderDate].As("OrderedDate")}
FROM {p_join.As("p")}
INNER JOIN {o.As("o")}
  ON {p_join[prod => prod.ItemNumber]} = {o[ord => ord.ProductItemNumber]}
WHERE {o[ord => ord.Quantity]} > {orderQuantity}
ORDER BY {o[ord => ord.OrderDate]} DESC");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
        Assert.Equal(orderQuantity, query.Parameters["@p0"]);
    }

    [Fact]
    public void FluentOnJoin_ShouldRenderCleanIndependentReferences()
    {
        // Arrange
        var p_join = Sql.GetTable<Product>();
        var o = Sql.GetTable<Order>();
        var orderQuantity = 10;
        var expectedSql = @"
SELECT 
  [p].[ItemNumber] AS [SKU],
  [p].[Name],
  [o].[OrderId] AS [OrderNumber],
  [o].[OrderDate] AS [OrderedDate]
FROM [dbo].[Product] AS [p]
INNER JOIN [dbo].[Orders] AS [o]
  ON [p].[ItemNumber] = [o].[ProductItemNumber]
WHERE [o].[Quantity] > @p0
ORDER BY [OrderedDate] DESC".Trim();

        // Act
        var query = Sql.Build($@"SELECT 
  {p_join[prod => prod.ItemNumber].As("SKU")},
  {p_join[prod => prod.Name]},
  {o[ord => ord.OrderId].As("OrderNumber")},
  {o[ord => ord.OrderDate].As("OrderedDate")}
FROM {p_join.As("p").InnerJoin(o.As("o")).On(p_join[prod => prod.ItemNumber], o[ord => ord.ProductItemNumber])}
WHERE {o[ord => ord.Quantity]} > {orderQuantity}
ORDER BY {o[ord => ord.OrderDate]} DESC");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
    }

    [Fact]
    public void Join_WithCustomIndent_ShouldFormatWithCorrectSpaces()
    {
        // Arrange
        var p_join = Sql.GetTable<Product>();
        var o = Sql.GetTable<Order>();
        var orderQuantity = 10;
        var options = new SqlInterpolOptions
        {
            Dialect = SqlDialect.SqlServer,
            IndentSize = 4 // 4 spaces instead of the default 2
        };

        var expectedSql = @"
SELECT 
    [p].[ItemNumber] AS [SKU],
    [p].[Name]
FROM [dbo].[Product] AS [p]
INNER JOIN [dbo].[Orders] AS [o]
    ON [p].[ItemNumber] = [o].[ProductItemNumber]
WHERE [o].[Quantity] > @p0".Trim();

        // Act
        var query = Sql.Build(options, $@"SELECT 
    {p_join[prod => prod.ItemNumber].As("SKU")},
    {p_join[prod => prod.Name]}
FROM {p_join.As("p").InnerJoin(o.As("o")).On(p_join[prod => prod.ItemNumber], o[ord => ord.ProductItemNumber])}
WHERE {o[ord => ord.Quantity]} > {orderQuantity}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
    }

    [Fact]
    public void JoinWithComplexOnConditions_ShouldRenderAndParameterizeCorrectly()
    {
        // Arrange
        var p_join = Sql.GetTable<Product>();
        var o = Sql.GetTable<Order>();
        var minOrderDate = new DateTime(2026, 03, 27); // Fixed date for test stability
        var highValue = 500m;
        var orderQuantity = 10;
        var expectedSql = @"
SELECT
  [p].[ItemNumber] AS [SKU],
  [p].[Name],
  [o].[OrderDate] AS [OrderedDate]
FROM [dbo].[Product] AS [p]
INNER JOIN [dbo].[Orders] AS [o]
  ON [p].[ItemNumber] = [o].[ProductItemNumber]
  AND [o].[OrderDate] > @p0
  OR [p].[Price] > @p1
WHERE [o].[Quantity] > @p2
ORDER BY [OrderedDate] DESC".Trim();

        // Act
        var query = Sql.Build($@"SELECT
  {p_join[prod => prod.ItemNumber].As("SKU")},
  {p_join[prod => prod.Name]},
  {o[ord => ord.OrderDate].As("OrderedDate")}
FROM {p_join.As("p")
    .InnerJoin(o.As("o"))
    .On(p_join[prod => prod.ItemNumber], o[ord => ord.ProductItemNumber])
    .And(Sql.Format("{0} > {1}", o[ord => ord.OrderDate], minOrderDate))
    .Or(Sql.Format("{0} > {1}", p_join[prod => prod.Price], highValue))}
WHERE {o[ord => ord.Quantity]} > {orderQuantity}
ORDER BY {Sql.Format("{0} DESC", o[ord => ord.OrderDate])}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
        Assert.Equal(3, query.Parameters.Count);
        Assert.Equal(minOrderDate, query.Parameters["@p0"]);
        Assert.Equal(highValue, query.Parameters["@p1"]);
        Assert.Equal(orderQuantity, query.Parameters["@p2"]);
    }

    [Fact]
    public void MultipleSqlFormatCalls_ShouldMaintainSequentialParameterIndexing()
    {
        // Arrange
        var p2 = Sql.GetTable<Product>();
        var priceFloor = 100m;
        var priceCeiling = 1000m;
        var expectedSql = @"
SELECT [p].[ItemNumber]
FROM [dbo].[Product] AS [p]
WHERE [p].[Price] >= @p0
AND [p].[Price] <= @p1".Trim();

        // Act
        var query = Sql.Build($@"SELECT {p2[prod => prod.ItemNumber]}
FROM {p2.As("p")}
WHERE {Sql.Format("{0} >= {1}", p2[prod => prod.Price], priceFloor)}
AND {Sql.Format("{0} <= {1}", p2[prod => prod.Price], priceCeiling)}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
        Assert.Equal(priceFloor, query.Parameters["@p0"]);
        Assert.Equal(priceCeiling, query.Parameters["@p1"]);
    }

    [Fact]
    public void SimpleInlineAlias_ShouldRenderCorrectManualSyntax()
    {
        // Arrange
        var p1 = Sql.GetTable<Product>();
        var minPrice = 100m;
        var name = "Widget";
        var expectedSql = @"
SELECT
  [p].[ItemNumber] AS [SKU],
  [p].[Name]
FROM [dbo].[Product] AS [p]
WHERE [p].[Price] > @p0
AND [p].[Name] = @p1
ORDER BY [p].[Price] DESC".Trim();

        // Act
        var query = Sql.Build($@"SELECT
  {p1[prod => prod.ItemNumber]} AS {p1[prod => prod.ItemNumber].Alias("SKU")},
  {p1[prod => prod.Name]}
FROM {p1} AS {p1.Alias("p")}
WHERE {p1[prod => prod.Price]} > {minPrice}
AND {p1[prod => prod.Name]} = {name}
ORDER BY {p1[prod => prod.Price]} DESC");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
        Assert.Equal(minPrice, query.Parameters["@p0"]);
        Assert.Equal(name, query.Parameters["@p1"]);
    }

    [Fact]
    public void CommentsAboveClauses_ShouldBePreservedInOutput()
    {
        // Arrange
        var p3 = Sql.GetTable<Product>();
        var minPrice = 100m;

        var expectedSql = @"
-- Select high value items
SELECT
  [p].[ItemNumber],
  [p].[Name]
-- Filter to active products only
FROM [dbo].[Product] AS [p]
-- Only include expensive items
WHERE [p].[Price] > @p0
-- Sort by price descending
ORDER BY [p].[Price] DESC".Trim();

        // Act
        var query = Sql.Build($@"-- Select high value items
SELECT
  {p3[prod => prod.ItemNumber]},
  {p3[prod => prod.Name]}
-- Filter to active products only
FROM {p3.As("p")}
-- Only include expensive items
WHERE {p3[prod => prod.Price]} > {minPrice}
-- Sort by price descending
ORDER BY {p3[prod => prod.Price]} DESC");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
    }

    [Fact]
    public void InlineComments_ShouldBePreservedNextToExpressions()
    {
        // Arrange
        var p3 = Sql.GetTable<Product>();
        var minPrice = 100m;

        var expectedSql = @"
SELECT
  [p].[ItemNumber] AS [SKU], -- Product identifier
  [p].[Name], -- Display name
  [p].[Price] -- Unit price
FROM [dbo].[Product] AS [p]
WHERE [p].[Price] > @p0".Trim();

        // Act
        var query = Sql.Build($@"SELECT
  {p3[prod => prod.ItemNumber]} AS [SKU], -- Product identifier
  {p3[prod => prod.Name]}, -- Display name
  {p3[prod => prod.Price]} -- Unit price
FROM {p3.As("p")}
WHERE {p3[prod => prod.Price]} > {minPrice}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
    }

    [Fact]
    public void BlockComments_ShouldBePreservedAndAttached()
    {
        // Arrange
        var p3 = Sql.GetTable<Product>();
        var minPrice = 100m;

        var expectedSql = @"
/* * Multi-line block comment
*/
SELECT
  [p].[ItemNumber],
  [p].[Name]
FROM [dbo].[Product] AS [p]
/* Filter criteria */
WHERE [p].[Price] > @p0".Trim();

        // Act
        var query = Sql.Build($@"/* * Multi-line block comment
*/
SELECT
  {p3[prod => prod.ItemNumber]},
  {p3[prod => prod.Name]}
FROM {p3.As("p")}
/* Filter criteria */
WHERE {p3[prod => prod.Price]} > {minPrice}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
    }

    [Fact]
    public void ArbitraryOrderClauses_ShouldReorderCorrectlyWithComments()
    {
        // Arrange
        var p3 = Sql.GetTable<Product>();
        var minPrice = 100m;

        // Even though C# defines ORDER BY first, SQL must render SELECT first.
        var expectedSql = @"
-- Product selection
SELECT
  [p].[ItemNumber],
  [p].[Name]
-- Source table
FROM [dbo].[Product] AS [p]
-- Filtering
WHERE [p].[Price] > @p0
/* Get expensive products sorted by price */
ORDER BY [p].[Price] DESC".Trim();

        // Act
        var query = Sql.Build($@"/* Get expensive products sorted by price */
ORDER BY {p3[prod => prod.Price]} DESC
-- Product selection
SELECT
  {p3[prod => prod.ItemNumber]},
  {p3[prod => prod.Name]}
-- Filtering
WHERE {p3[prod => prod.Price]} > {minPrice}
-- Source table
FROM {p3.As("p")}");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
    }

    [Fact]
    public void CommentsAtEnd_ShouldBeAppendedToFinalSql()
    {
        // Arrange
        var p3 = Sql.GetTable<Product>();
        var minPrice = 100m;
        var expectedSql = @"
SELECT
  [p].[ItemNumber],
  [p].[Name]
FROM [dbo].[Product] AS [p]
WHERE [p].[Price] > @p0
-- Query logged for audit trail
-- Generated for reporting dashboard".Trim();

        // Act
        var query = Sql.Build($@"SELECT
  {p3[prod => prod.ItemNumber]},
  {p3[prod => prod.Name]}
FROM {p3.As("p")}
WHERE {p3[prod => prod.Price]} > {minPrice}
-- Query logged for audit trail
-- Generated for reporting dashboard");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
    }

//     TODO: Fix sub queries
//     [Fact]
//     public void SubqueryInWhere_ShouldWrapInParenthesesAndMergeParams()
//     {
//         // Arrange
//         var p7 = Sql.GetTable<Product>();
//         var avgPrice = 50m;
        
//         // Create the inner query first
//         var subquery = Sql.Build($@"
// SELECT AVG({p7[prod => prod.Price]})
// FROM {p7}
// WHERE {p7[prod => prod.Price]} > {avgPrice}");

//         // Act
//         var finalQuery = Sql.Build($@"SELECT {p7[prod => prod.ItemNumber]}, {p7[prod => prod.Name]}
// FROM {p7}
// WHERE {p7[prod => prod.Price]} > {subquery}");

//         // Assert
//         // Note: The library handles the indentation of the subquery
//         var expectedSql = @"
// SELECT [dbo].[Product].[ItemNumber], [dbo].[Product].[Name]
// FROM [dbo].[Product]
// WHERE [dbo].[Product].[Price] > (SELECT AVG([dbo].[Product].[Price])
// FROM [dbo].[Product]
// WHERE [dbo].[Product].[Price] > @p0)".Trim();

//         Assert.Equal(expectedSql, finalQuery.Sql);
//         Assert.Equal(avgPrice, finalQuery.Parameters["@p0"]);
//     }

//     [Fact]
//     public void SubqueryInFrom_ShouldSupportAliasingAndOuterReferences()
//     {
//         // Arrange
//         var p8 = Sql.GetTable<Product>();
//         var threshold = 50m;

//         // Act
//         var query = Sql.Build($@"-- Top products by price
// SELECT t.[ItemNumber], t.[Name], t.[HighPrice]
// FROM (
//   SELECT 
//     {p8[prod => prod.ItemNumber]},
//     {p8[prod => prod.Name]},
//     {p8[prod => prod.Price]} AS [HighPrice]
//   FROM {p8}
//   WHERE {p8[prod => prod.Price]} > {threshold}
// ) AS t
// ORDER BY t.[HighPrice] DESC");

//         // Assert
//         var expectedSql = @"
// -- Top products by price
// SELECT t.[ItemNumber], t.[Name], t.[HighPrice]
// FROM (
//   SELECT 
//     [dbo].[Product].[ItemNumber],
//     [dbo].[Product].[Name],
//     [dbo].[Product].[Price] AS [HighPrice]
//   FROM [dbo].[Product]
//   WHERE [dbo].[Product].[Price] > @p0
// ) AS [t]
// ORDER BY t.[HighPrice] DESC".Trim();

//         Assert.Equal(expectedSql, query.Sql);
//         Assert.Equal(threshold, query.Parameters["@p0"]);
//     }

//     [Fact]
//     public void NestedSubqueries_ShouldMergeAndRenumberParametersRecursively()
//     {
//         // Arrange
//         var p9 = Sql.GetTable<Product>();
//         var threshold = 100m;
        
//         // Step 1: Create the inner query
//         var innerSubquery = Sql.Build($@"SELECT {p9[prod => prod.ItemNumber]} 
// FROM {p9}
// WHERE {p9[prod => prod.Price]} > {threshold}");

//         // Act
//         // Step 2: Nest it inside another query
//         var finalQuery = Sql.Build($@"SELECT COUNT(*) AS [ExpensiveCount]
// FROM (
//   SELECT {p9[prod => prod.ItemNumber]}
//   FROM {p9}
//   WHERE {p9[prod => prod.ItemNumber]} IN (
//     {innerSubquery}
//   )
// ) AS expensive_products");

//         // Assert
//         var expectedSql = @"
// SELECT COUNT(*) AS [ExpensiveCount]
// FROM (
//   SELECT [dbo].[Product].[ItemNumber]
//   FROM [dbo].[Product]
//   WHERE [dbo].[Product].[ItemNumber] IN (
//     (SELECT [dbo].[Product].[ItemNumber] 
// FROM [dbo].[Product]
// WHERE [dbo].[Product].[Price] > @p0)
// )
// ) AS [expensive_products]".Trim();

//         Assert.Equal(expectedSql, finalQuery.Sql);
//         Assert.Single(finalQuery.Parameters);
//         Assert.Equal(threshold, finalQuery.Parameters["@p0"]);
//     }

    [Fact]
    public void SubqueryInJoin_ShouldAutoAliasAndQuoteCorrectly()
    {
        // Arrange
        var p10 = Sql.GetTable<Product>();
        var o10 = Sql.GetTable<Order>();
        var joinThreshold = 50m;
        var lastMonth = new DateTime(2026, 03, 26); // Match console-test.txt

        var expensiveProducts = Sql.Build($@"SELECT {p10[prod => prod.ItemNumber]}
FROM {p10}
WHERE {p10[prod => prod.Price]} > {joinThreshold}");

        var expensiveProductsProduct = expensiveProducts.Project<Product>();

        // Act
        var query = Sql.Build($@"SELECT {p10[prod => prod.ItemNumber]} AS SKU, {o10[ord => ord.OrderDate]} AS Ordered
FROM {o10} AS o
INNER JOIN {expensiveProducts} AS expensive_prods
  ON {o10[ord => ord.ProductItemNumber]} = {expensiveProductsProduct[ep => ep.ItemNumber]}
WHERE {o10[ord => ord.OrderDate]} > {lastMonth}
ORDER BY {o10[ord => ord.OrderDate]} DESC");

        // Assert
        var expectedSql = @"
SELECT [dbo].[Product].[ItemNumber] AS [SKU], [o].[OrderDate] AS [Ordered]
FROM [dbo].[Orders] AS [o]
INNER JOIN (
  SELECT [dbo].[Product].[ItemNumber]
  FROM [dbo].[Product]
  WHERE [dbo].[Product].[Price] > @p0
) AS [expensive_prods]
  ON [o].[ProductItemNumber] = [expensive_prods].[ItemNumber]
WHERE [Ordered] > @p1
ORDER BY [Ordered] DESC".Trim();

        Assert.Equal(expectedSql, query.Sql);
        Assert.Equal(joinThreshold, query.Parameters["@p0"]);
        Assert.Equal(lastMonth, query.Parameters["@p1"]);
    }

    [Fact]
    public void CastSafety_ShouldNotMistakeCastTypeForAlias()
    {
        // Arrange
        var p10c = Sql.GetTable<Product>();
        
        var expectedSql = @"
SELECT CAST([products].[ItemNumber] AS BIGINT) AS [ConvertedSKU]
FROM [dbo].[Product] AS [products]".Trim();

        // Act
        var query = Sql.Build($@"SELECT CAST({p10c[prod => prod.ItemNumber]} AS BIGINT) AS ConvertedSKU
FROM {p10c} AS products");

        // Assert
        Assert.Equal(expectedSql, query.Sql);
    }

    [Theory]
    [InlineData(SqlDialect.SqlServer, "[SKU]", "[expensive_prods]")]
    [InlineData(SqlDialect.PostgreSql, "\"SKU\"", "\"expensive_prods\"")]
    [InlineData(SqlDialect.MySql, "`SKU`", "`expensive_prods`")]
    public void DialectAliasing_ShouldQuoteAliasesBasedOnDatabaseType(SqlDialect dialect, string expectedColAlias, string expectedSubqueryAlias)
    {
        // Arrange
        var p10d = Sql.GetTable<Product>();
        var subquery = Sql.Build(dialect, $"SELECT {p10d[x => x.ItemNumber]} FROM {p10d}");

        // Act
        var query = Sql.Build(dialect, $@"
SELECT {p10d[x => x.ItemNumber]} AS SKU
FROM ({subquery}) AS expensive_prods");

        // Assert
        Assert.Contains($"AS {expectedColAlias}", query.Sql);
        Assert.Contains($"AS {expectedSubqueryAlias}", query.Sql);
    }

    [Fact]
    public void LazyAliasResolution_ShouldResolveAliasDefinedLaterInString()
    {
        // Arrange
        var p10 = Sql.GetTable<Product>();
        var o10 = Sql.GetTable<Order>();
        var expProducts = Sql.Build($"SELECT {p10[x => x.ItemNumber]} FROM {p10}");
        var expProdRef = expProducts.Project<Product>();
        var lastMonth = new DateTime(2026, 03, 26);

        // Act
        // Note how expProdRef is used in the ON clause BEFORE the subquery is aliased
        var query = Sql.Build($@"
FROM {o10} AS o
  ON {o10[ord => ord.ProductItemNumber]} = {expProdRef[ep => ep.ItemNumber]}
SELECT
  {p10[prod => prod.ItemNumber]},
  {o10[ord => ord.OrderDate]}
INNER JOIN {expProducts} AS expensive_prods
WHERE {o10[ord => ord.OrderDate]} > {lastMonth}");

        // Assert
        // The columns in the SELECT and ON clauses should correctly use [expensive_prods]
        // even though the alias was "declared" later in the builder's execution.
        Assert.Contains("ON [o].[ProductItemNumber] = [expensive_prods].[ItemNumber]", query.Sql);
    }

    [Fact]
    public void OneLineQuery_ShouldRenderCorrectAliasingAndOrdering()
    {
        // Arrange
        var minPrice = 100m;
        var expectedSql = "SELECT [p].[ItemNumber] AS [SKU] FROM [dbo].[Product] AS [p] WHERE [p].[Price] > @p0 ORDER BY [SKU] ASC";

        // Act
        var query = Sql.SqlQuery<Product>(table => Sql.Build($@"SELECT {table[p => p.ItemNumber].As("SKU")} FROM {table.As("p")} WHERE {table[p => p.Price]} > {minPrice} ORDER BY {table[p => p.ItemNumber]} ASC"));

        // Assert
        // We check the exact string. Note: your console output shows this as a single line.
        Assert.Equal(expectedSql, query.Sql);
        Assert.Single(query.Parameters);
        Assert.Equal(minPrice, query.Parameters["@p0"]);
    }
}