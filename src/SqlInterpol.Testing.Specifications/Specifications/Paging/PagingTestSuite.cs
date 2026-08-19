using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IPagingTestSuite))]
public abstract partial class PagingTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IPagingTestSuite.Paging_WithImplicitLimitOffsetData))]
    public void Paging_WithImplicitLimitOffset(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        int pageSize = 10;
        int pageOffset = 20;

        // Act - Using standard PostgreSQL/MySQL syntax. The engine transpiles it for others!
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}, {{p.Name}}
                FROM {{p}}
                ORDER BY {{p.Id}}
                LIMIT {{pageSize}} OFFSET {{pageOffset}}
                """).Build();
        });

        // Assert - Natively verifies the SQL string AND the expected parameters array!
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}