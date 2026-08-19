using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IRawSqlTestSuite))]
public abstract partial class RawSqlTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IRawSqlTestSuite.ComplexRawSqlData))]
    public void RawSql_ComplexStatements(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var minPrice = 50.00m;

#pragma warning disable SQLIG10
        // Act
        // We are mixing entity references {{p}}, parameters {{minPrice}}, and RAW SQL here!
        testCase.Act(() =>
        db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}, {{p.Name}}
            FROM {{p}}
            WHERE {{p.Price}} > {{minPrice}}
              {{Sql.Raw("AND p.Status = 'ACTIVE'")}} /* Raw SQL condition */
            GROUP BY {{p.Id}}, {{p.Name}}
            HAVING COUNT(*) > 1
            ORDER BY {{p.Name}} DESC
            LIMIT 10 OFFSET 5
            """)
            .Build()
        );
#pragma warning restore SQLIG10

        testCase.Assert();
    }

    [SqlTest(nameof(IRawSqlTestSuite.WindowFunctionData))]
    public void RawSql_WindowFunctions(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        // Act
        // Proving that window functions and raw SQL keywords flow perfectly around our interpolation tags
#pragma warning disable SQLIG10
        testCase.Act(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT 
                {{p.Name}},
                {{p.Price}},
                AVG({{p.Price}}) OVER (PARTITION BY {{p.CategoryId}}) AS AvgCategoryPrice
            FROM {{p}}
            """)
            .Build()
        );
#pragma warning restore SQLIG10

        testCase.Assert();
    }
}