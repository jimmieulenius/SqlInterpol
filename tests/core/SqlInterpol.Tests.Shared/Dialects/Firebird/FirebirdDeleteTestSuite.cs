using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdDeleteTestSuite : IDeleteTestSuite
{
    private static readonly object[] _pureManualParams = [42];
    private static readonly object[] _multiTableParams = [];
    private static readonly object[] _templateParams = [1];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> DeletePureManualData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" = @p0
                """
            ],
            expectedParameters: _pureManualParams
        )
    ];

    public static TheoryData<SqlTestCase> DeleteMultiTableData =>
    [
        new SqlTestCase(
            expectedExceptionType: typeof(SqlDialectException),
            expectedExceptionMessage: "The SQL dialect 'Firebird' does not support the operation or fragment type: 'Multi-Table DELETE'."
        )
    ];

    public static TheoryData<SqlTestCase> DeleteTemplateData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "Users"
                WHERE "Users"."Id" = @p0
                """
            ],
            expectedParameters: _templateParams
        )
    ];
}