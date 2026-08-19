using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
using SqlInterpol.Schema;
using SqlInterpol.Segments;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ISegmentRewriterTestSuite))]
public abstract partial class SegmentRewriterTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(ISegmentRewriterTestSuite.SoftDeleteData))]
    public void Pipeline_CustomSoftDeleteRewriter(SqlTestCase testCase)
    {
        // Arrange
        // We create fresh options and inject the custom AST Rewriter. 
        // Because the properties are nullable, this won't break the dialect's native defaults!
        var options = new SqlInterpolOptions();
        options.Rewriters.Add(new SoftDeleteRewriter()); 

        var db = CreateBuilder(options);
        int targetId = 42;

        // Act: The user writes a standard DELETE statement
        testCase.Act(() => 
        {
            db.Entity<Order>(out var o);
            return db.Append($$"""
                DELETE FROM {{o}}
                WHERE {{o.Id}} = {{targetId}}
                """).Build();
        });

        // Assert: The engine automatically intercepted and rewrote it across ALL dialects!
        testCase.Assert();
    }
}