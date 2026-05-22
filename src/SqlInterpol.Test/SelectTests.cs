using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class SelectTests
{
    [Theory]
    [MemberData(nameof(SelectExpansionData))]
    public void Select_EntityExpansion_ExpandsColumnsAndAppliesAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<ProductWithIgnoreModel>(p => db.Append($$"""
            SELECT {{p}}
            FROM {{p}} AS p1
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(SingleColumnData))]
    public void Select_SingleColumn(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}}
            FROM {{p}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(MultipleColumnsData))]
    public void Select_MultipleColumns(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Id]}},
                {{p[x => x.CategoryId]}}
            FROM {{p}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(SqlFunctionData))]
    public void Select_SqlFunction(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                COUNT({{p[x => x.Id]}})
            FROM {{p}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(LiteralParameterData))]
    public void Select_LiteralParameter(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int activeStatus = 1;

        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{activeStatus}}
            FROM {{p}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
        
        // Verify the parameter was captured correctly
        Assert.Single(result.Parameters);
        Assert.Equal(1, result.Parameters.Values.First());
    }

    [Theory]
    [MemberData(nameof(CustomColumnAttributeData))]
    public void Select_CustomColumnAttribute(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<Product>(p =>
            db.Append($$"""
            SELECT
                {{p[x => x.Name]}}
            FROM {{p}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(SelectDistinctVerticalLayoutData))]
    public void Select_Distinct_EntityExpansion_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;

        // Act
        var result = db.Query<ProductWithIgnoreModel>(p => db.Append($$"""
            SELECT DISTINCT {{p}}
            FROM {{p}} AS p1
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(TopKeywordData))]
    public void Select_TopKeyword_PassesThrough(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT TOP 10 {{p[x => x.Id]}}
            FROM {{p}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(SelectComplexData))]
    public void SelectPromotion_OnlyProjectsScalarColumns(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act - Ensure the entity resolves properly with an alias
        var result = db.Query<ComplexProduct>(p => db.Append($$"""
            SELECT {{p}}
            FROM {{p}} AS {{Sql.Quote("p")}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        
        // Extra paranoia check to ensure the complex type didn't bleed into the SQL
        Assert.DoesNotContain("Supplier", result.Sql);
    }

    public static TheoryData<SqlTestCase> SelectExpansionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT p1.<<Id>>, p1.<<PROD_NAME>>
                FROM <<dbo>>.<<Products>> AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT p1."Id", p1."PROD_NAME"
                FROM "dbo"."Products" AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT p1.`Id`, p1.`PROD_NAME`
                FROM `dbo`.`Products` AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT p1."Id", p1."PROD_NAME"
                FROM "dbo"."Products" AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT p1."Id", p1."PROD_NAME"
                FROM "dbo"."Products" AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT p1."Id", p1."PROD_NAME"
                FROM "dbo"."Products" AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT p1.[Id], p1.[PROD_NAME]
                FROM [dbo].[Products] AS p1
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> SingleColumnData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<dbo>>.<<Products>>.<<Id>>
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "dbo"."Products"."Id"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
                    `dbo`.`Products`.`Id`
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT
                    "dbo"."Products"."Id"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT
                    "dbo"."Products"."Id"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "dbo"."Products"."Id"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [dbo].[Products].[Id]
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> MultipleColumnsData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT
                    <<dbo>>.<<Products>>.<<Id>>,
                    <<dbo>>.<<Products>>.<<CategoryId>>
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "dbo"."Products"."Id",
                    "dbo"."Products"."CategoryId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
                    `dbo`.`Products`.`Id`,
                    `dbo`.`Products`.`CategoryId`
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "dbo"."Products"."Id",
                    "dbo"."Products"."CategoryId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "dbo"."Products"."Id",
                    "dbo"."Products"."CategoryId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    "dbo"."Products"."Id",
                    "dbo"."Products"."CategoryId"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [dbo].[Products].[Id],
                    [dbo].[Products].[CategoryId]
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> SqlFunctionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT
                    COUNT(<<dbo>>.<<Products>>.<<Id>>)
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    COUNT("dbo"."Products"."Id")
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
                    COUNT(`dbo`.`Products`.`Id`)
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    COUNT("dbo"."Products"."Id")
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    COUNT("dbo"."Products"."Id")
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    COUNT("dbo"."Products"."Id")
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    COUNT([dbo].[Products].[Id])
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> LiteralParameterData =>
    [
        // Note: Replace the parameter prefix (@, :, etc.) based on your dialect's default setup
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT
                    !!100
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    @p0
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    @p0
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    :0
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    $1
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT
                    @p1
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    @p0
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> CustomColumnAttributeData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<dbo>>.<<Products>>.<<PROD_NAME>>
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT
                    `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT
                    "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT
                    "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            [
                """
                SELECT
                    "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT
                    [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> SelectDistinctVerticalLayoutData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT DISTINCT
                    p1.<<Id>>,
                    p1.<<PROD_NAME>>
                FROM <<dbo>>.<<Products>> AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT DISTINCT
                    p1."Id",
                    p1."PROD_NAME"
                FROM "dbo"."Products" AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT DISTINCT
                    p1.`Id`,
                    p1.`PROD_NAME`
                FROM `dbo`.`Products` AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT DISTINCT
                    p1."Id",
                    p1."PROD_NAME"
                FROM "dbo"."Products" AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT DISTINCT
                    p1."Id",
                    p1."PROD_NAME"
                FROM "dbo"."Products" AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT DISTINCT
                    p1."Id",
                    p1."PROD_NAME"
                FROM "dbo"."Products" AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT DISTINCT
                    p1.[Id],
                    p1.[PROD_NAME]
                FROM [dbo].[Products] AS p1
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> TopKeywordData =>
    [
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT TOP 10 [dbo].[Products].[Id]
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> SelectComplexData =>
    [
        new SqlTestCase
        (
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<p>>.<<Id>>, <<p>>.<<Name>>, <<p>>.<<Status>>, <<p>>.<<Category>>
                FROM <<tbl_complex_products>> AS <<p>>
                """
            ]
        ),
        new SqlTestCase
        (
            SqlDialectKind.Firebird,
            [
                """
                SELECT "p"."Id", "p"."Name", "p"."Status", "p"."Category"
                FROM "tbl_complex_products" AS "p"
                """
            ] 
        ),
        new SqlTestCase
        (
            SqlDialectKind.MySql,
            [
                """
                SELECT `p`.`Id`, `p`.`Name`, `p`.`Status`, `p`.`Category`
                FROM `tbl_complex_products` AS `p`
                """
            ]
        ),
        new SqlTestCase
        (
            SqlDialectKind.Oracle,
            [
                """
                SELECT "p"."Id", "p"."Name", "p"."Status", "p"."Category"
                FROM "tbl_complex_products" AS "p"
                """
            ]
        ),
        new SqlTestCase
        (
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "p"."Id", "p"."Name", "p"."Status", "p"."Category"
                FROM "tbl_complex_products" AS "p"
                """
            ]
        ),
        new SqlTestCase
        (
            SqlDialectKind.SqLite,
            [
                """
                SELECT "p"."Id", "p"."Name", "p"."Status", "p"."Category"
                FROM "tbl_complex_products" AS "p"
                """
            ]
        ),
        new SqlTestCase
        (
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [p].[Id], [p].[Name], [p].[Status], [p].[Category]
                FROM [tbl_complex_products] AS [p]
                """
            ]
        )
    ];
}