using System.Collections.Generic;
using System.Linq;
using Xunit;
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
            SELECTAAA TOPZZZ 10 {{p[x => x.Id]}}
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

    [Theory]
    [MemberData(nameof(BuiltInTemplateSelectWithCriteriaData))]
    public void Select_BuiltInTemplate_WithCriteria(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var criteria = new { Id = 42 };

        // Act - Leverages global static pre-compiled Select cache pipelines
        var result = db.Query<Product>(p =>
            db.Append(SqlTemplate.Select<Product, ProductTemplateDto>(x => x.Id), p, criteria)
        ).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Single(result.Parameters);
        Assert.Equal(42, result.Parameters.Values.First());
    }

    [Theory]
    [MemberData(nameof(CustomTemplateSelectData))]
    public void Select_CustomTemplate(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var template = SqlTemplate.Create<Product>((builder, p) =>
        {
            builder.Append($"SELECT {p[x => x.Id]}, {p[x => x.Name]} FROM {p} WHERE {p[x => x.CategoryId]} = {Sql.Arg<Product>(x => x.CategoryId)}");
        });
        var args = new { CategoryId = 5 };

        // Act - Maps runtime identifiers over frozen custom layouts
        var result = db.Query<Product>(p =>
            db.Append(template, p, args)
        ).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Single(result.Parameters);
        Assert.Equal(5, result.Parameters.Values.First());
    }

    [Theory]
    [MemberData(nameof(TemplateFragmentSelectData))]
    public void Select_TemplateFragment(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var selectTemplate = SqlTemplate.Select<Product, ProductTemplateDto>();
        var categoryId = 100;

        // Act
        var result = db.Query<Product>(p =>
            db.AppendLine(selectTemplate, p)
            .Append($"WHERE {p[x => x.CategoryId]} = {categoryId}")
        ).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Single(result.Parameters);
        Assert.Equal(100, result.Parameters.Values.First());
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
                SELECT <<p>>.<<Category>>, <<p>>.<<Id>>, <<p>>.<<Name>>, <<p>>.<<Status>>
                FROM <<tbl_complex_products>> AS <<p>>
                """
            ]
        ),
        new SqlTestCase
        (
            SqlDialectKind.Firebird,
            [
                """
                SELECT "p"."Category", "p"."Id", "p"."Name", "p"."Status"
                FROM "tbl_complex_products" AS "p"
                """
            ] 
        ),
        new SqlTestCase
        (
            SqlDialectKind.MySql,
            [
                """
                SELECT `p`.`Category`, `p`.`Id`, `p`.`Name`, `p`.`Status`
                FROM `tbl_complex_products` AS `p`
                """
            ]
        ),
        new SqlTestCase
        (
            SqlDialectKind.Oracle,
            [
                """
                SELECT "p"."Category", "p"."Id", "p"."Name", "p"."Status"
                FROM "tbl_complex_products" AS "p"
                """
            ]
        ),
        new SqlTestCase
        (
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "p"."Category", "p"."Id", "p"."Name", "p"."Status"
                FROM "tbl_complex_products" AS "p"
                """
            ]
        ),
        new SqlTestCase
        (
            SqlDialectKind.SqLite,
            [
                """
                SELECT "p"."Category", "p"."Id", "p"."Name", "p"."Status"
                FROM "tbl_complex_products" AS "p"
                """
            ]
        ),
        new SqlTestCase
        (
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [p].[Category], [p].[Id], [p].[Name], [p].[Status]
                FROM [tbl_complex_products] AS [p]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> BuiltInTemplateSelectWithCriteriaData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<IsActive>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
                FROM <<dbo>>.<<Products>>
                WHERE <<dbo>>.<<Products>>.<<Id>> = !!100
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Id" = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`IsActive`, `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`Id` = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Id" = :0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Id" = $1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Id" = @p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[IsActive], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products]
                WHERE [dbo].[Products].[Id] = @p0
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> CustomTemplateSelectData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<PROD_NAME>> FROM <<dbo>>.<<Products>> WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME` FROM `dbo`.`Products` WHERE `dbo`.`Products`.`CategoryId` = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = :0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = $1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME" FROM "dbo"."Products" WHERE "dbo"."Products"."CategoryId" = @p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME] FROM [dbo].[Products] WHERE [dbo].[Products].[CategoryId] = @p0
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> TemplateFragmentSelectData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>, <<dbo>>.<<Products>>.<<IsActive>>, <<dbo>>.<<Products>>.<<PROD_NAME>>
                FROM <<dbo>>.<<Products>>
                WHERE <<dbo>>.<<Products>>.<<CategoryId>> = !!100
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."CategoryId" = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`IsActive`, `dbo`.`Products`.`PROD_NAME`
                FROM `dbo`.`Products`
                WHERE `dbo`.`Products`.`CategoryId` = @p0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."CategoryId" = :0
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."CategoryId" = $1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."IsActive", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."CategoryId" = @p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id], [dbo].[Products].[IsActive], [dbo].[Products].[PROD_NAME]
                FROM [dbo].[Products]
                WHERE [dbo].[Products].[CategoryId] = @p0
                """
            ]
        )
    ];
}