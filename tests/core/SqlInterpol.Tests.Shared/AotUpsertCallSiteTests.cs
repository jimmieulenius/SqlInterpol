using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests;

/// <summary>
/// AOT call-site proof for handwritten UPSERT. Spec suites own the full dialect matrix
/// (<see cref="UpsertTestSuite"/>); Append there is compiled in Specifications without
/// interceptors. These Shared tests are linked into Aot/Jit hosts so
/// <see cref="SqlBuilderTestExtensions.AssertAotIntercepted"/> is meaningful.
/// Expected SQL must stay aligned with SqlServer/PostgreSql <c>UpsertData</c>.
/// </summary>
public class AotUpsertCallSiteTests
{
    [Fact]
    public void Handwritten_OnConflict_SqlServer_AotCallSite()
    {
        var testCase = new SqlTestCase(
            expectedSql: [
                """
                MERGE INTO [dbo].[Products] AS target
                USING (VALUES (@p0, @p1, @p2, @p3)) AS source([Id], [PROD_NAME], [CategoryId], [Price])
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                  UPDATE SET target.[PROD_NAME] = @p4, target.[Price] = @p5
                WHEN NOT MATCHED THEN
                  INSERT ([Id], [PROD_NAME], [CategoryId], [Price])
                  VALUES (source.[Id], source.[PROD_NAME], source.[CategoryId], source.[Price]);
                """
            ],
            expectedParameters: [42, "Apple", 1, 10m, "Apple", 10m]
        );

        var options = new SqlInterpolOptions { CrossDialectSqlTranspilation = true };
        var db = SqlBuilder.SqlServer(options);
        var newProduct = new { Id = 42, Name = "Apple", CategoryId = 1, Price = 10m };
        var updateProduct = new { Name = "Apple", Price = 10m };

        testCase.Act(() =>
        {
            db.Entity<UpsertTestSuite.Product>(out var p);
            return db.Append($$"""
                INSERT INTO {{p}} {{newProduct}}
                ON CONFLICT {{p.Id}}
                DO UPDATE SET {{updateProduct}}
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [Fact]
    public void Handwritten_OnConflict_PostgreSql_AotCallSite()
    {
        var testCase = new SqlTestCase(
            expectedSql: [
                """
                INSERT INTO "dbo"."Products" ("Id", "PROD_NAME", "CategoryId", "Price")
                VALUES ($1, $2, $3, $4)
                ON CONFLICT ("Id")
                DO UPDATE SET "PROD_NAME" = $5, "Price" = $6
                """
            ],
            expectedParameters: [42, "Apple", 1, 10m, "Apple", 10m]
        );

        var options = new SqlInterpolOptions { CrossDialectSqlTranspilation = true };
        var db = SqlBuilder.PostgreSql(options);
        var newProduct = new { Id = 42, Name = "Apple", CategoryId = 1, Price = 10m };
        var updateProduct = new { Name = "Apple", Price = 10m };

        testCase.Act(() =>
        {
            db.Entity<UpsertTestSuite.Product>(out var p);
            return db.Append($$"""
                INSERT INTO {{p}} {{newProduct}}
                ON CONFLICT {{p.Id}}
                DO UPDATE SET {{updateProduct}}
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }
}
