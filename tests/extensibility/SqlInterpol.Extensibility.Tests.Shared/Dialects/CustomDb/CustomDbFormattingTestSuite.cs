using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Tests.Dialects.CustomDb;

public partial class CustomDbFormattingTestSuite : IFormattingTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => 
#if CSHARP14_EXTENSION_TYPES
        SqlBuilder.CustomDb(options);
#else
        SqlBuilderFactory.CustomDb(options);
#endif

    public static TheoryData<SqlTestCase> Select_WithNewLinesData => [new SqlTestCase([
        """
        SELECT 
            <<dbo>>.<<Products>>.<<Id>>, 
            <<dbo>>.<<Products>>.<<PROD_NAME>>
        FROM 
            <<dbo>>.<<Products>>
        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithTabsData => [new SqlTestCase([
        """
        SELECT  <<dbo>>.<<Products>>.<<Id>>,  <<dbo>>.<<Products>>.<<PROD_NAME>>
        FROM  <<dbo>>.<<Products>>
        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithExtraSpacesData => [new SqlTestCase([
        """
        SELECT <<dbo>>.<<Products>>.<<Id>>
          FROM <<dbo>>.<<Products>>
         WHERE <<dbo>>.<<Products>>.<<Id>> = 1
        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithMixedWhitespaceData => [new SqlTestCase([
        """

            SELECT <<dbo>>.<<Products>>.<<Id>>
            FROM <<dbo>>.<<Products>>

        """
    ])];

    public static TheoryData<SqlTestCase> Select_WithCommentsData => [new SqlTestCase([
        """
        SELECT <<dbo>>.<<Products>>.<<Id>> -- This is the primary key
        FROM <<dbo>>.<<Products>> /* This is the table */
        """
    ])];

    public static TheoryData<SqlTestCase> InsertVerticalLayoutData => [new SqlTestCase([
        """
        INSERT INTO <<dbo>>.<<Orders>>
        (
            <<order_status>>,
            <<Total>>
        )
        VALUES
        (
            !!100,
            !!101
        )
        """
    ])];

    public static TheoryData<SqlTestCase> UpdateVerticalLayoutData => [new SqlTestCase([
        """
        UPDATE <<dbo>>.<<Orders>>
        SET
            <<order_status>> = !!100,
            <<Total>> = !!101
        """
    ])];

    public static TheoryData<SqlTestCase> BulkInsertVerticalLayoutData => [new SqlTestCase([
        """
        INSERT INTO <<dbo>>.<<Products>>
        (
            <<PROD_NAME>>,
            <<CategoryId>>,
            <<Price>>
        )
        VALUES
        (
            !!100,
            !!101,
            !!102
        ),
        (
            !!103,
            !!104,
            !!105
        )
        """
    ])];

    public static TheoryData<SqlTestCase> WhereInVerticalLayoutData => [new SqlTestCase([
        """
        SELECT *
        FROM <<dbo>>.<<Orders>>
        WHERE <<dbo>>.<<Orders>>.<<Id>> IN (
            !!100,
            !!101,
            !!102
        )
        """
    ])];

    public static TheoryData<SqlTestCase> OrderByEnumerableVerticalLayoutData => [new SqlTestCase([
        """
        SELECT *
        FROM <<dbo>>.<<Orders>>
        ORDER BY 
            <<dbo>>.<<Orders>>.<<Total>>,
            <<dbo>>.<<Orders>>.<<Id>> DESC
        """
    ])];

    public static TheoryData<SqlTestCase> SelectEntityExpansionVerticalLayoutData => [new SqlTestCase([
        """
        SELECT
            <<p1>>.<<Id>>,
            <<p1>>.<<PROD_NAME>>
        FROM <<dbo>>.<<Products>> AS <<p1>>
        """
    ])];
}