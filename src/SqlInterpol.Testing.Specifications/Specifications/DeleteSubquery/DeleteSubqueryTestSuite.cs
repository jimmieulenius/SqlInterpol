using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IDeleteSubqueryTestSuite))]
public abstract partial class DeleteSubqueryTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    protected const string Status = "Cancelled";

    [SqlTest(nameof(IDeleteSubqueryTestSuite.Delete_WithSubqueryData))]
    public void Delete_WithSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<OrderLine>(out var l)
            .Entity<OrderModel>(out var o);

            
            return db.Append($$"""
                DELETE FROM {{l}}
                WHERE {{l.OrderId}} IN (
                    SELECT {{o.Id}}
                    FROM {{o}}
                    WHERE {{o.Status}} = {{Status}}
                )
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}