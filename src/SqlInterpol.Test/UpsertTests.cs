using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class UpsertTests
{
    [Theory]
    [MemberData(nameof(UpsertData))]
    public void Upsert_CrossDialect(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var newProduct = new { Id = 42, Name = "Apple", CategoryId = 1, Price = 10m };
        var updateProduct = new { Name = "Apple", Price = 10m };

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}} {{newProduct}}
            ON CONFLICT {{p[x => x.Id]}}
            DO UPDATE SET {{updateProduct}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(OnDuplicateKeyData))]
    public void Upsert_OnDuplicateKey_PassesThrough(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var id = 1;
        var newPrice = 99.99m;

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}} (Id, Price) 
            VALUES ({{id}}, {{newPrice}}) 
            ON DUPLICATE KEY UPDATE Price = {{newPrice}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Equal(3, result.Parameters.Count);
    }

    [Theory]
    [MemberData(nameof(OnConflictData))]
    public void Upsert_OnConflictDoNothing_PassesThrough(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var id = 1;

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}} (Id) 
            VALUES ({{id}}) 
            ON CONFLICT DO NOTHING
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Single(result.Parameters);
    }

    public static TheoryData<SqlTestCase> UpsertData =>
    [
        // MySQL converts to ON DUPLICATE KEY UPDATE and drops "SET"
        new SqlTestCase(SqlDialectKind.MySql, [
            """
            INSERT INTO `dbo`.`Products` (`Id`, `PROD_NAME`, `CategoryId`, `Price`)
            VALUES (@p0, @p1, @p2, @p3)
            ON DUPLICATE KEY UPDATE `PROD_NAME` = @p4, `Price` = @p5
            """
        ]),
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
            VALUES (@p1, @p2, @p3, @p4)
            ON CONFLICT ("Id")
            DO UPDATE SET "PROD_NAME" = @p5, "Price" = @p6
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

    public static TheoryData<SqlTestCase> OnDuplicateKeyData =>
    [
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Products` (Id, Price) 
                VALUES (@p0, @p1) 
                ON DUPLICATE KEY UPDATE Price = @p2
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> OnConflictData =>
    [
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                INSERT INTO "dbo"."Products" (Id) 
                VALUES ($1) 
                ON CONFLICT DO NOTHING
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                INSERT INTO "dbo"."Products" (Id) 
                VALUES (@p1) 
                ON CONFLICT DO NOTHING
                """
            ]
        )
    ];
}