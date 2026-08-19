using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.MySql;

public partial class MySqlTemplateTestSuite : ITemplateTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.MySql(options);

    public static TheoryData<SqlTestCase> TemplateSelectData => [new SqlTestCase(
        expectedSql: [
            """
            SELECT `o1`.`Id`, `o1`.`CustomerId`
            FROM `dbo`.`Orders` AS `o1`
            WHERE `o1`.`CustomerId` = @p0
            ORDER BY `o1`.`Id` DESC
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateBulkInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO `dbo`.`Orders` (`Id`)
            VALUES (@p0), (@p1)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE `dbo`.`Orders` SET `CustomerId` = @p0 WHERE `Id` = @p1;
            UPDATE `dbo`.`Orders` SET `CustomerId` = @p2 WHERE `Id` = @p3
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateDeleteData => [new SqlTestCase(
        expectedSql: [
            """
            DELETE FROM `dbo`.`Orders` WHERE `Id` = @p0;
            DELETE FROM `dbo`.`Orders` WHERE `Id` = @p1
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualInsertData => [new SqlTestCase(
        expectedSql: [
            """
            INSERT INTO `dbo`.`Orders`
            (`Id`, `CustomerId`)
            VALUES (@p0, @p1)
            """
        ]
    )];

    public static TheoryData<SqlTestCase> TemplateManualUpdateData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE `dbo`.`Orders`
            SET `CustomerId` = @p0
            WHERE `Id` = @p1
            """
        ]
    )];
}