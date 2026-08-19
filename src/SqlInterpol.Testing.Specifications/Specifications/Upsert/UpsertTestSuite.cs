using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IUpsertTestSuite))]
public abstract partial class UpsertTestSuite
{
    protected static readonly TestUser TemplateUser = new() { Id = 1, Name = "Charlie", Age = 32 };

    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IUpsertTestSuite.UpsertData))]
    public void Upsert_CrossDialect(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var newProduct = new { Id = 42, Name = "Apple", CategoryId = 1, Price = 10m };
        var updateProduct = new { Name = "Apple", Price = 10m };

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);

            #pragma warning disable SQLIG10
            return db.Append($$"""
                INSERT INTO {{p}} {{newProduct}}
                ON CONFLICT {{p.Id}}
                DO UPDATE SET {{updateProduct}}
                """).Build();
            #pragma warning restore SQLIG10
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IUpsertTestSuite.OnDuplicateKeyData))]
    public void Upsert_OnDuplicateKey(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var id = 1;
        var newPrice = 99.99m;

        testCase.Act(() => 
        {
            #pragma warning disable SQLIG10
            db.Entity<Product>(out var p);
            return db.Append($$"""
                INSERT INTO {{p}} (Id, Price)
                VALUES ({{id}}, {{newPrice}})
                ON DUPLICATE KEY UPDATE Price = {{newPrice}}
                """).Build();
            #pragma warning restore SQLIG10
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IUpsertTestSuite.OnConflictData))]
    public void Upsert_OnConflictDoNothing(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var id = 1;

        testCase.Act(() => 
        {
            #pragma warning disable SQLIG10
            db.Entity<Product>(out var p);
            return db.Append($$"""
                INSERT INTO {{p}} (Id)
                VALUES ({{id}})
                ON CONFLICT DO NOTHING
                """).Build();
            #pragma warning restore SQLIG10
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IUpsertTestSuite.UpsertTemplateData))]
    public void Upsert_Template(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() => 
        {
            db.Entity<TestUser>(out var u);
            // AppendUpsert natively handles dialing translation!
            return db.AppendUpsert(u, u.Id, TemplateUser).Build();
        });

        testCase.Assert();
    }
}