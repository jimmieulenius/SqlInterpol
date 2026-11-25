using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteDeleteTestSuiteBase : IDeleteTestSuite
{
    private static readonly object[] _pureManualParams = [42];
    private static readonly object[] _multiTableParams = [];
    private static readonly object[] _templateParams = [1];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> DeletePureManualData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "Orders"
                WHERE "Orders"."Id" = @p1
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
                DELETE FROM "Products"
                FROM "Category" AS "c1"
                WHERE "Products"."CategoryId" = c1.Id
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
                WHERE "Users"."Id" = @p1
                """
            ],
            expectedParameters: _templateParams
        )
    ];
}