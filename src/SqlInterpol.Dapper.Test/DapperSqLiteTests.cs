// using Dapper;
// using Microsoft.Data.Sqlite;
// using SqlInterpol;
// using SqlInterpol.Dapper;

// namespace SqlInterpol.IntegrationTests;

// public class DapperSqLiteTests
// {
//     // An in-memory database persists as long as the connection remains open
//     private const string ConnectionString = "Data Source=:memory:";

//     [Fact]
//     public void Verify_Dapper_Executes_GeneratedSqLiteSql()
//     {
//         // Arrange
//         var product = new TestProduct
//         {
//             Name = "Laptop",
//             Price = 999.99m,
//             ActiveStatus = 1,
//             Category = "Electronics"
//         };

//         // Setup database connection & Schema
//         using var connection = new SqliteConnection(ConnectionString);
//         connection.Open();

//         // Create a dummy table for local execution testing
//         var db = connection.CreateSqlBuilder();
//         var p = db.AddEntity<TestProduct>();

//         var query = db.Append($$"""
//             CREATE TABLE {{p}} (
//                 {{p[x => x.Id]}} INTEGER PRIMARY KEY,
//                 {{p[x => x.Name]}} TEXT NOT NULL,
//                 {{p[x => x.Price]}} REAL NOT NULL,
//                 {{p[x => x.ActiveStatus]}} INTEGER NOT NULL,
//                 {{p[x => x.Category]}} TEXT NOT NULL
//             );
//             INSERT INTO {{p}}
//             VALUES {{product}};
//             """)
//             .Build();
//         connection.Execute(query);

//         // Act
//         int activeStatus = 1;
//         string category = "Electronics";

//         query = db.Append($$"""
//             SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}, {{p[x => x.Price]}}
//             FROM {{p}}
//             WHERE {{p[x => x.ActiveStatus]}} = {{activeStatus}} AND {{p[x => x.Category]}} = {{category}}
//             """)
//             .Build();
//         var products = connection.Query<TestProduct>(query).ToList();

//         var existsQuery = db.Append($$"""
//             SELECT 1 
//             FROM sqlite_master 
//             WHERE type = 'table' AND name = '{{Sql.Raw(p.Name)}}'
//             """).Build();
//         var tableExistsBefore = connection.ExecuteScalar<bool>(existsQuery);

//         query = db.Append($$"""
//             DROP TABLE IF EXISTS {{p}};
//             """)
//             .Build();
//         connection.Execute(query);
        
//         var tableExistsAfter = connection.ExecuteScalar<bool>(existsQuery);

//         // Assert
//         Assert.Single(products);
//         Assert.Equal(product.Name, products[0].Name);
//         Assert.Equal(product.Price, products[0].Price);
//         Assert.True(tableExistsBefore, "The test table should exist before dropping.");
//         Assert.False(tableExistsAfter, "The test table should have been dropped successfully.");
//     }

//     [SqlTable("tbl_products")]
//     private class TestProduct
//     {
//         [SqlColumn("id")]
//         public int Id { get; set; }
//         [SqlColumn("name")]
//         public string Name { get; set; } = string.Empty;
//         [SqlColumn("price")]
//         public decimal Price { get; set; }
//         [SqlColumn("category_name")]
//         public string Category { get; set; } = string.Empty;
//         [SqlColumn("status_id")]
//         public int ActiveStatus { get; set; }
//     }
// }