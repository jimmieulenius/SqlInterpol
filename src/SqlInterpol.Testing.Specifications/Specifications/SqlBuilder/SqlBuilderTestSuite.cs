using System;
using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ISqlBuilderTestSuite))]
public abstract partial class SqlBuilderTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(ISqlBuilderTestSuite.AppendData))]
    public void SqlBuilder_Append(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($"SELECT {p.Id}").Append($" FROM {p}").Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISqlBuilderTestSuite.AppendLineData))]
    public void SqlBuilder_AppendLine(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.AppendLine($"SELECT {p.Id}")
                     .Append($"FROM {p}")
                     .Build();
        });
    
        testCase.Assert();
    }

    [SqlTest(nameof(ISqlBuilderTestSuite.RawStringData))]
    public void SqlBuilder_RawString(SqlTestCase testCase)
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
}