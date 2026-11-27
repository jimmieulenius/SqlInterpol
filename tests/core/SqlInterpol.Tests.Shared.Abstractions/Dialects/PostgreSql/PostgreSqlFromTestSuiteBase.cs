using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;
using static SqlInterpol.Testing.Specifications.FromTestSuite;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public abstract partial class PostgreSqlFromTestSuiteBase : IFromTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> From_SingleEntityData => [
        new SqlTestCase(
            expectedSql: [
                """
                SELECT *
                FROM "OrderLine"
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> From_EntityWithSqlTableNameOnlyData => [
        new SqlTestCase(
            expectedSql: [
                """
                SELECT *
                FROM "MyTable"
                """
            ],
            expectedData: new SqlTestDbData 
            {
                Data = [
                    new TableOnlyModel(Id: 1)
                ]
            }
        )
    ];

    public static TheoryData<SqlTestCase> From_Entity_WithSqlTableNameAndSchemaData => [
        new SqlTestCase(
            expectedSql: [
                """
                SELECT *
                FROM "MySchema"."MyTable"
                """
            ],
            expectedData: new SqlTestDbData 
            {
                Data = [
                    new TableAndSchemaModel(Id: 1)
                ]
            }
        )
    ];
}