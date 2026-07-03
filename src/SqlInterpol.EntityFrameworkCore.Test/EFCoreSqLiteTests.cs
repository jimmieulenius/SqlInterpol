using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqlInterpol.EFCore;

namespace SqlInterpol.IntegrationTests;

public class EFCoreSqLiteTests
{
    // For EF Core in-memory SQLite, the connection must be opened 
    // and held open, otherwise the database vanishes instantly.
    private const string ConnectionString = "Data Source=:memory:";

    [Fact]
    public void Verify_EFCore_Executes_GeneratedSqLiteSql()
    {
        // Arrange
        // Setup Connection & DbContext
        var product = new TestProduct
        {
            Id = 1,
            Name = "Laptop",
            Price = 999.99m,
            ActiveStatus = 1,
            Category = "Electronics"
        };

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);

        // Setup Schema & Insert Data via our extension
        var db = context.CreateSqlBuilder();
        
        // 1. Proxy Registration
        db.Entity<TestProduct>(out var p);

        // 2. DDL Generation using the :col format specifier to extract bare column names
        var createQuery = db.Append($$"""
            CREATE TABLE {{p}} (
                {{p.Id:col}} INTEGER PRIMARY KEY AUTOINCREMENT,
                {{p.Name:col}} TEXT NOT NULL,
                {{p.Price:col}} REAL NOT NULL,
                {{p.ActiveStatus:col}} INTEGER NOT NULL,
                {{p.Category:col}} TEXT NOT NULL
            );
            """).Build();

        context.ExecuteSql(createQuery);

        // 3. Insert using the Auto-CRUD engine
        var insertQuery = db.AppendInsert(p, product).Build();
        context.ExecuteSql(insertQuery);

        // Act
        // Query Data back out
        int activeStatus = 1;
        string category = "Electronics";

        // NOTE: EF Core requires all mapped columns to be returned by FromSql!
        // We list them all or use `SELECT {{p}}` to ensure EF Core can track the entity.
        var selectQuery = db.Append($$"""
            SELECT {{p}}
            FROM {{p}}
            WHERE {{p.ActiveStatus}} = {{activeStatus}} AND {{p.Category}} = {{category}}
            """).Build();

        // Natively return tracked entities using the new EF Core bridge!
        var productEntity = context.FromSql<TestProduct>(selectQuery).ToList().Single();
        var entityStateBefore = context.Entry(productEntity).State;
        var updateName = "Updated Laptop";

        productEntity.Name = updateName;
        
        var entityStateAfter = context.Entry(productEntity).State;

        // Assert
        Assert.NotNull(productEntity);
        Assert.Equal(updateName, productEntity.Name);
        Assert.Equal(999.99m, productEntity.Price);
        Assert.Equal(category, productEntity.Category);
        Assert.Equal(activeStatus, productEntity.ActiveStatus);
        
        // Ensure EF Core is actually tracking it
        Assert.Equal(EntityState.Unchanged, entityStateBefore);
        Assert.Equal(EntityState.Modified, entityStateAfter);
    }

    [Fact]
    public void MapEntity_SynchronizesEnums_AndIgnoresComplexTypes_InEFCore()
    {
        // 1. Arrange In-Memory SQLite Database
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);

        // 2. Verify the EF Core Model Metadata BEFORE we execute
        var entityType = context.Model.FindEntityType(typeof(AppUser));
        Assert.NotNull(entityType);
        
        // Ensure complex type was ignored as a standard column property
        var profileProperty = entityType.FindProperty(nameof(AppUser.Profile));
        Assert.Null(profileProperty); 

        // Verify Enum Value Converters
        var roleProperty = entityType.FindProperty(nameof(AppUser.Role));
        var stateProperty = entityType.FindProperty(nameof(AppUser.State));
        
        Assert.NotNull(roleProperty);
        Assert.NotNull(stateProperty);
        
        // Role should default to integer (EF Core native behavior means ProviderClrType is null or int)
        Assert.True(roleProperty.GetProviderClrType() == null || roleProperty.GetProviderClrType() == typeof(int));
        
        // State MUST have been flagged as a string by our MapEntity<T> extension!
        Assert.Equal(typeof(string), stateProperty.GetProviderClrType());

        // 3. Act - Create schema and insert data using the EF Context
        context.Database.EnsureCreated(); // EF Core creates tables based on MapEntity
        
        var user = new AppUser
        {
            Id = 1,
            Username = "TestUser",
            Role = Role.Admin,
            State = AccountState.Active,
            Profile = new UserProfile { Bio = "Hello World" }
        };

        var db = context.CreateSqlBuilder();
        db.Entity<AppUser>(out var u);

        // We can completely replace the manual INSERT with AppendInsert!
        // The SqlMetadataRegistry automatically intersects the DTO properties with 
        // the database columns, completely ignoring the complex 'Profile' object natively!
        var insertQuery = db.AppendInsert(u, user).Build();
        context.ExecuteSql(insertQuery);

        // 4. Assert - Retrieve using standard EF Core FromSql
        var selectQuery = db.Append($$"""
            SELECT {{u}} FROM {{u}}
            """).Build();

        var retrievedUsers = context.FromSql<AppUser>(selectQuery).ToList();

        Assert.Single(retrievedUsers);
        Assert.Equal(Role.Admin, retrievedUsers[0].Role);
        Assert.Equal(AccountState.Active, retrievedUsers[0].State);
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

    public enum Role { Admin = 1, User = 2 }
    public enum AccountState { Active = 1, Suspended = 2 }
    
    public class UserProfile { public string Bio { get; set; } = string.Empty; }

    [SqlTable("tbl_users")]
    public class AppUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        
        // Defaults to Integer
        public Role Role { get; set; }
        
        // Forced to String via SqlInterpol
        [SqlEnumFormat(SqlEnumFormat.String)]
        public AccountState State { get; set; }
        
        // Complex object (Should be ignored by MapEntity)
        public UserProfile Profile { get; set; } = new();
    }

    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<TestProduct> Products { get; set; }
        public DbSet<AppUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.MapSqlEntity<TestProduct>()
                .MapSqlEntity<AppUser>()
                .Entity<AppUser>().Ignore(x => x.Profile);
        }
    }
}