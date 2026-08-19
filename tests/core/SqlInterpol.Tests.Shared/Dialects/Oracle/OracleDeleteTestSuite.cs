using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleDeleteTestSuite : IDeleteTestSuite
{
    private static readonly object[] _pureManualParams = [42];
    private static readonly object[] _multiTableParams = [];
    private static readonly object[] _templateParams = [1];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> DeletePureManualData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" = :0
                """
            ],
            expectedParameters: _pureManualParams
        )
    ];

    public static TheoryData<SqlTestCase> DeleteMultiTableData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "dbo"."Products"
                WHERE EXISTS (
                    SELECT 1
                    FROM "Category" "c1"
                    WHERE "dbo"."Products"."CategoryId" = c1.Id
                )
                """
            ],
            expectedParameters: _multiTableParams
        )
    ];

    public static TheoryData<SqlTestCase> DeleteTemplateData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "Users"
                WHERE "Users"."Id" = :0
                """
            ],
            expectedParameters: _templateParams
        )
    ];
}