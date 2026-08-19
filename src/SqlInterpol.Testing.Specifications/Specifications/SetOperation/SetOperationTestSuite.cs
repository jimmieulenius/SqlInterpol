using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ISetOperationTestSuite))]
public abstract partial class SetOperationTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(ISetOperationTestSuite.QueryIntersectData))]
    public void Intersect(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}} FROM {{p}}
                INTERSECT
                SELECT {{p.Id}} FROM {{p}} WHERE {{p.CategoryId}} = {{1}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISetOperationTestSuite.QueryIntersectData))]
    public void Intersect_Query(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        // We only declare the entity scope ONCE for the entire test!
        db.Entity<Product>(out var p);
        
        // Build isolated modular fragments using the shared scope
        var q1 = db.Fragment($"SELECT {p.Id} FROM {p}");
        var q2 = db.Fragment($"SELECT {p.Id} FROM {p} WHERE {p.CategoryId} = {1}");

        testCase.Act(() => 
        {
            return db.Append($$"""
                {{q1}}
                INTERSECT
                {{q2}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISetOperationTestSuite.QueryExceptData))]
    public void Except(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}} FROM {{p}}
                EXCEPT
                SELECT {{p.Id}} FROM {{p}} WHERE {{p.CategoryId}} = {{2}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISetOperationTestSuite.QueryExceptData))]
    public void Except_Query(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        db.Entity<Product>(out var p);
        
        // Build isolated modular fragments
        var q1 = db.Fragment($"SELECT {p.Id} FROM {p}");
        var q2 = db.Fragment($"SELECT {p.Id} FROM {p} WHERE {p.CategoryId} = {2}");

        testCase.Act(() => 
        {
            return db.Append($$"""
                {{q1}}
                EXCEPT
                {{q2}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISetOperationTestSuite.Select_UnionAllData))]
    public void Select_UnionAll(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        int cat1 = 1;
        int cat2 = 2;

        db.Entity<Product>(out var p);

        // Build the first query
        var query1 = db.Fragment($$"""
            SELECT {{p.Id}}, {{p.Name}}
            FROM {{p}}
            WHERE {{p.CategoryId}} = {{cat1}}
            """);

        // Build the second query
        var query2 = db.Fragment($$"""
            SELECT {{p.Id}}, {{p.Name}}
            FROM {{p}}
            WHERE {{p.CategoryId}} = {{cat2}}
            """);

        // Act - Pure WYSIWYG Union!
        testCase.Act(() => 
        {
            return db.Append($$"""
                {{query1}}
                UNION ALL
                {{query2}}
                """).Build();
        });

        testCase.Assert();
    }
}