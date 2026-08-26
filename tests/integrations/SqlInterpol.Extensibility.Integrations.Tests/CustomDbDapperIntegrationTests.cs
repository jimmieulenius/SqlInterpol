using SqlInterpol.Configuration;
using SqlInterpol.Dapper;
using SqlInterpol.Extensibility.Dialects;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Integrations.Tests;

public class CustomDbDapperIntegrationTests : IDisposable
{
    public CustomDbDapperIntegrationTests()
    {
        // Arrange: Register the CustomDb dialect for our dummy connection type
        SqlInterpolDapperExtensions.ConnectionResolvers.Add(ResolveConnection);
    }

    public void Dispose()
    {
        // Cleanup: Remove the resolver so it doesn't bleed into other tests
        SqlInterpolDapperExtensions.ConnectionResolvers.Remove(ResolveConnection);
    }

    private ISqlDialect? ResolveConnection(Type type) =>
        type.Name == nameof(CustomDbConnection) ? new CustomDbSqlDialect() : null;

    [Fact]
    public void CreateSqlBuilder_FromDapperConnection_ShouldResolve_CustomDbDialect()
    {
        // Arrange
        using var connection = new CustomDbConnection();
        var testCase = new SqlTestCase(
            expectedSql: [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>
                FROM <<dbo>>.<<Products>>
                WHERE <<dbo>>.<<Products>>.<<Id>> = !!100
                """
            ],
            expectedParameters: [42]
        );

        // Act
        testCase.Act(() => 
        {
            // The Dapper extension method automatically resolves CustomDbSqlDialect
            var db = connection.CreateSqlBuilder();
            
            db.Entity<Product>(out var p);
            return db.Append(
                $$"""
                SELECT {{p.Id}}
                FROM {{p}}
                WHERE {{p.Id}} = {{42}}
                """).Build();
        });

        // Assert: Validates exact SQL structure, quoting, and parameter indexing
        testCase.Assert();
    }
}