using SqlInterpol.Config;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class UpsertTests
{
    [Theory]
    [MemberData(nameof(AllDialectsUpsertData))]
    public void Upsert_CrossDialect_RendersCorrectly(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        
        var newProduct = new { Id = 42, Name = "Apple", CategoryId = 1, Price = 10m };
        var updateProduct = new { Name = "Apple", Price = 10m };

        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}} {{newProduct}}
            ON CONFLICT {{p[x => x.Id]}}
            DO UPDATE SET {{updateProduct}}
            """)).Build();

        var expectedSql = testCase.ExpectedSql[0].Replace("\r\n", "\n").Trim();
        var actualSql = result.Sql.Replace("\r\n", "\n").Trim();
        
        Assert.Equal(expectedSql, actualSql);
    }

    public static TheoryData<SqlTestCase> AllDialectsUpsertData =>
    [
        // Postgres & SQLite pass through natively
        new SqlTestCase(SqlDialectKind.PostgreSql, [
            """
            INSERT INTO "dbo"."Products" ("Id", "PROD_NAME", "CategoryId", "Price")
            VALUES ($1, $2, $3, $4)
            ON CONFLICT ("Id")
            DO UPDATE SET "PROD_NAME" = $5, "Price" = $6
            """
        ]),
        new SqlTestCase(SqlDialectKind.SqLite, [
            """
            INSERT INTO "dbo"."Products" ("Id", "PROD_NAME", "CategoryId", "Price")
            VALUES (?0, ?1, ?2, ?3)
            ON CONFLICT ("Id")
            DO UPDATE SET "PROD_NAME" = ?4, "Price" = ?5
            """
        ]),
        // MySQL converts to ON DUPLICATE KEY UPDATE and drops "SET"
        new SqlTestCase(SqlDialectKind.MySql, [
            """
            INSERT INTO `dbo`.`Products` (`Id`, `PROD_NAME`, `CategoryId`, `Price`)
            VALUES (@p0, @p1, @p2, @p3)
            ON DUPLICATE KEY UPDATE `PROD_NAME` = @p4, `Price` = @p5
            """
        ]),
        // SQL Server intercepts the ENTIRE syntax tree and generates a perfect ANSI MERGE statement
        new SqlTestCase(SqlDialectKind.SqlServer, [
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
        ])
    ];
}