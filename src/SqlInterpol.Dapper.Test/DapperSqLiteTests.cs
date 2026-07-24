using Dapper;
using Microsoft.Data.Sqlite;
using SqlInterpol;
using SqlInterpol.Dapper;
using SqlInterpol.Schema;

namespace SqlInterpol.IntegrationTests;

public class DapperSqLiteTests
{
    // An in-memory database persists as long as the connection remains open
    private const string ConnectionString = "Data Source=:memory:";

    [Fact]
    public void Verify_Dapper_Executes_GeneratedSqLiteSql()
    {
        // Arrange
        var product = new TestProduct
        {
            Id = 1, // SQLite needs an ID for the DTO insertion
            Name = "Laptop",
            Price = 999.99m,
            ActiveStatus = 1,
            Category = "Electronics"
        };

        // Setup database connection & Schema
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var db = connection.CreateSqlBuilder();
        
        // Use the new highly-typed proxy registration
        db.Entity<TestProduct>(out var p);

        // 1. Create a dummy table for local execution testing
        // We use the :col format specifier to cleanly extract just the column name (e.g. 'id') 
        // without the table prefix for the CREATE TABLE definition!
        var createQuery = db.Append($$"""
            CREATE TABLE {{p}} (
                {{p.Id:col}} INTEGER PRIMARY KEY,
                {{p.Name:col}} TEXT NOT NULL,
                {{p.Price:col}} REAL NOT NULL,
                {{p.ActiveStatus:col}} INTEGER NOT NULL,
                {{p.Category:col}} TEXT NOT NULL
            );
            """).Build();
        
        connection.Execute(createQuery);

        // 2. Insert the dummy data using our globally cached auto-CRUD engine
        var insertQuery = db.AppendInsert(p, product).Build();
        connection.Execute(insertQuery);

        // Act
        int activeStatus = 1;
        string category = "Electronics";

        // 3. Query the data using the direct entity properties
        var selectQuery = db.Append($$"""
            SELECT {{p.Id}}, {{p.Name}}, {{p.Price}}
            FROM {{p}}
            WHERE {{p.ActiveStatus}} = {{activeStatus}} AND {{p.Category}} = {{category}}
            """).Build();
            
        var products = connection.Query<TestProduct>(selectQuery).ToList();

        // 4. Verify table existence 
        // (Hardcoding 'tbl_products' since p.Name would access the proxy's string property!)
        var existsQuery = db.Append($$"""
            SELECT 1 
            FROM sqlite_master 
            WHERE type = 'table' AND name = 'tbl_products'
            """).Build();
            
        var tableExistsBefore = connection.ExecuteScalar<bool>(existsQuery);

        // 5. Cleanup
        var dropQuery = db.Append($$"""
            DROP TABLE IF EXISTS {{p}};
            """).Build();
            
        connection.Execute(dropQuery);
        
        var tableExistsAfter = connection.ExecuteScalar<bool>(existsQuery);

        // Assert
        Assert.Single(products);
        Assert.Equal(product.Name, products[0].Name);
        Assert.Equal(product.Price, products[0].Price);
        Assert.True(tableExistsBefore, "The test table should exist before dropping.");
        Assert.False(tableExistsAfter, "The test table should have been dropped successfully.");
    }

    [SqlTable("tbl_products")]
    public class TestProduct
    {
        [SqlColumn("id")]
        public int Id { get; set; }
        [SqlColumn("name")]
        public string Name { get; set; } = string.Empty;
        [SqlColumn("price")]
        public decimal Price { get; set; }
        [SqlColumn("category_name")]
        public string Category { get; set; } = string.Empty;
        [SqlColumn("status_id")]
        public int ActiveStatus { get; set; }
    }
}