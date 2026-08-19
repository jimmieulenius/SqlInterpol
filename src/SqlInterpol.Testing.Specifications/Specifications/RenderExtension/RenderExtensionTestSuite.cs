using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IRenderExtensionTestSuite))]
public abstract partial class RenderExtensionTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IRenderExtensionTestSuite.AsDeclarationData))]
    public void RenderExtension_AsDeclaration(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p, "prod");
            return db.Append($"SELECT * FROM {p.AsDeclaration()}").Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IRenderExtensionTestSuite.AsAliasData))]
    public void RenderExtension_AsAlias(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p, "prod");
            return db.Append($"SELECT {p.AsAlias()}.* FROM dbo.Products AS {p.AsAlias()}").Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IRenderExtensionTestSuite.AsBaseData))]
    public void RenderExtension_AsBase(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p, "prod");
            return db.Append($"TRUNCATE TABLE {p.AsBase()}").Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IRenderExtensionTestSuite.AsColumnData))]
    public void RenderExtension_AsColumn(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p, "prod");
            return db.Append($"SELECT {p.Name.AsColumn()} FROM {p}").Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IRenderExtensionTestSuite.CombinedData))]
    public void RenderExtension_Combined(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p, "prod");
            
            // Declare without an explicit alias; the tool's inline "AS" parser will automatically link it
            db.Entity<Product>(out var backup);
            
            return db.Append($$"""
                SELECT 
                    {{p.Id.AsColumn()}}, 
                    {{p.AsAlias()}}.{{p.Name.AsColumn()}}
                FROM {{p.AsDeclaration()}}
                INNER JOIN {{backup}} AS backup_prod ON {{backup.AsAlias()}}.Id = {{p.AsAlias()}}.Id
                """).Build();
        });

        testCase.Assert();
    }
}