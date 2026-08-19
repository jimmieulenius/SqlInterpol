using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public partial class PostgreSqlDeleteTestSuite : IDeleteTestSuite
{
    private static readonly object[] _pureManualParams = [42];
    private static readonly object[] _multiTableParams = [];
    private static readonly object[] _templateParams = [1];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> DeletePureManualData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" = $1
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
                USING "Category" AS "c1"
                WHERE "dbo"."Products"."CategoryId" = c1.Id
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
                WHERE "Users"."Id" = $1
                """
            ],
            expectedParameters: _templateParams
        )
    ];
}