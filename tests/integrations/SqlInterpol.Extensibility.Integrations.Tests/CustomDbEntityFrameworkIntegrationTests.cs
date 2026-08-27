using Microsoft.EntityFrameworkCore;
using SqlInterpol.Dialects;
using SqlInterpol.EFCore;
using SqlInterpol.Extensibility.Dialects;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Integrations.Tests;

public class CustomDbDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("CustomDbIntegration");
    }
}

public class CustomDbEFCoreIntegrationTests : IDisposable
{
    public CustomDbEFCoreIntegrationTests()
    {
        // Arrange: Teach EF Core extensions to use CustomDb when it sees the InMemory provider
        SqlInterpolEFCoreExtensions.ProviderResolvers.Add(ResolveProvider);
    }

    public void Dispose()
    {
        // Cleanup: Remove the resolver so it doesn't bleed into other tests
        SqlInterpolEFCoreExtensions.ProviderResolvers.Remove(ResolveProvider);
    }

    private ISqlDialect? ResolveProvider(string providerName) =>
        providerName == "Microsoft.EntityFrameworkCore.InMemory" ? new CustomDbSqlDialect() : null;

    [Fact]
    public void CreateSqlBuilder_FromEFContext_ShouldResolve_CustomDbDialect()
    {
        // Arrange
        using var context = new CustomDbDbContext();
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
            var db = context.CreateSqlBuilder();
            
            db.Entity<Product>(out var p);
            return db.Append(
                $$"""
                SELECT {{p.Id}}
                FROM {{p}}
                WHERE {{p.Id}} = {{42}}
                """).Build();
        });

        // Assert
        testCase.Assert();
    }
}