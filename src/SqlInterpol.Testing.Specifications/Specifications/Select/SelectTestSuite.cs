using System.Linq;
using SqlInterpol.Configuration;
using SqlInterpol.Execution;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ISelectTestSuite))]
public abstract partial class SelectTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(ISelectTestSuite.SelectExpansionData))]
    public void Select_EntityExpansion(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() => 
        {
            db.Entity<ProductWithIgnoreModel>(out var p);
            return db.Append($$"""
                SELECT {{p}}
                FROM {{p}} AS p1
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectTestSuite.SingleColumnData))]
    public void Select_SingleColumn(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Id}}
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectTestSuite.MultipleColumnsData))]
    public void Select_MultipleColumns(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Id}},
                    {{p.CategoryId}}
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectTestSuite.SqlFunctionData))]
    public void Select_SqlFunction(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    COUNT({{p.Id}})
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectTestSuite.LiteralParameterData))]
    public void Select_LiteralParameter(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        int activeStatus = 1;
        SqlQueryResult? result = null;

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            result = db.Append($$"""
                SELECT
                    {{activeStatus}}
                FROM {{p}}
                """).Build();
            return result;
        });

        testCase.Assert();
        
        Assert.NotNull(result);
        Assert.Single(result.Parameters);
        Assert.Equal(1, result.Parameters.Values.First());
    }

    [SqlTest(nameof(ISelectTestSuite.CustomColumnAttributeData))]
    public void Select_CustomColumnAttribute(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Name}}
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectTestSuite.SelectDistinctVerticalLayoutData))]
    public void Select_Distinct_EntityExpansion_VerticalLayout(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;

        testCase.Act(() => 
        {
            db.Entity<ProductWithIgnoreModel>(out var p);
            return db.Append($$"""
                SELECT DISTINCT {{p}}
                FROM {{p}} AS p1
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectTestSuite.TopKeywordData))]
    public void Select_TopKeyword_PassesThrough(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT TOP 10 {{p.Id}}
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectTestSuite.SelectComplexData))]
    public void SelectPromotion_OnlyProjectsScalarColumns(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        SqlQueryResult? result = null;

        testCase.Act(() => 
        {
            db.Entity<ComplexProduct>(out var p);

#pragma warning disable SQLIG10 // Sql.Quote dynamically evaluates SQL fragments via JIT
            result = db.Append($$"""
                SELECT {{p}}
                FROM {{p}} AS {{Sql.Quote("p")}}
                """).Build();
#pragma warning restore SQLIG10

            return result;
        });

        testCase.Assert();
        // Omitted db.AssertAotIntercepted() because Sql.Quote forces JIT execution
        
        Assert.NotNull(result);
        Assert.DoesNotContain("Supplier", result.Sql);
    }
}