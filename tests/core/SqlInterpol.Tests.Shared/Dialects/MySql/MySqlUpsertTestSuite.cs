using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlUpsertTestSuite : IUpsertTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> UpsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO `dbo`.`Products` (`Id`, `PROD_NAME`, `CategoryId`, `Price`)
            VALUES (@p0, @p1, @p2, @p3)
            ON DUPLICATE KEY UPDATE `PROD_NAME` = @p4, `Price` = @p5
            """
        ],
        expectedParameters: [42, "Apple", 1, 10m, "Apple", 10m]
    )];

    public static TheoryData<SqlTestCase> OnDuplicateKeyData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO `dbo`.`Products` (Id, Price)
            VALUES (@p0, @p1)
            ON DUPLICATE KEY UPDATE Price = @p2
            """
        ],
        expectedParameters: [1, 99.99m, 99.99m]
    )];

    public static TheoryData<SqlTestCase> OnConflictData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT IGNORE INTO `dbo`.`Products` (Id)
            VALUES (@p0)
            """
        ],
        expectedParameters: [1]
    )];

    public static TheoryData<SqlTestCase> UpsertTemplateData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO `Users`
            (`Age`, `Id`, `Name`)
            VALUES (@p0, @p1, @p2)
            ON DUPLICATE KEY UPDATE `Age` = @p3, `Name` = @p4
            """
        ],
        expectedParameters: [32, 1, "Charlie", 32, "Charlie"]
    )];
}