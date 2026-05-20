using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class FormattingTests
{
    [Theory]
    [MemberData(nameof(Select_WithNewLinesData))]
    public void Select_WithNewLines(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        var result = db.Query<Product>(p => db.Append($$"""
            SELECT 
                {{p[x => x.Id]}}, 
                {{p[x => x.Name]}}
            FROM 
                {{p}}
            """)).Build();

        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(Select_WithTabsData))]
    public void Select_WithTabs(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        var result = db.Query<Product>(p => db.Append($$"""
            SELECT	{{p[x => x.Id]}},	{{p[x => x.Name]}}
            FROM	{{p}}
            """)).Build();

        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(Select_WithExtraSpacesData))]
    public void Select_WithExtraSpaces(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        // Testing right-aligned keywords (common in some style guides)
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}
              FROM {{p}}
             WHERE {{p[x => x.Id]}} = 1
            """)).Build();

        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(Select_WithMixedWhitespaceData))]
    public void Select_WithMixedWhitespace(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        // Testing a mix of leading/trailing blank lines and indentation
        var result = db.Query<Product>(p => db.Append($$"""

            SELECT {{p[x => x.Id]}}
            FROM {{p}}

            """)).Build();

        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(Select_WithCommentsData))]
    public void Select_WithComments(SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}} -- This is the primary key
            FROM {{p}} /* This is the table */
            """)).Build();

        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(InsertVerticalLayoutData))]
    public void Insert_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;
        var dto = new { Status = "New", Total = 10m };

        // Act - Using implicit contextual syntax!
        var result = db.Query<OrderModel>(o => 
            db.Append($$"""
                INSERT INTO {{o}}
                VALUES {{dto}}
                """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(UpdateVerticalLayoutData))]
    public void Update_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;
        var dto = new { Status = "Processing", Total = 50.00m };

        // Act - Using implicit contextual syntax!
        // Note: No trailing space after SET because the vertical collection prepends its own newline!
        var result = db.Query<OrderModel>(o => 
            db.Append($$"""
                UPDATE {{o}}
                SET {{dto}}
                """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(BulkInsertVerticalLayoutData))]
    public void BulkInsert_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;
        
        var products = new[]
        {
            new { Name = "Prod1", CategoryId = 1, Price = 10m },
            new { Name = "Prod2", CategoryId = 2, Price = 20m }
        };

        // Act
        var result = db.Query<Product>(p => 
            db.Append($$"""
                INSERT INTO {{p}}
                VALUES
                {{products}}
                """))
            .Build();

        // Assert        
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(WhereInVerticalLayoutData))]
    public void WhereIn_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;
        var ids = new[] { 1, 2, 3 };

        // Act
        var result = db.Query<OrderModel>(o => 
            db.Append($$"""
            SELECT *
            FROM {{o}}
            WHERE {{o[x => x.Id]}} IN (
                {{ids}}
            )
            """))   
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(OrderByEnumerableVerticalLayoutData))]
    public void OrderBy_EnumerableCombiner_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Force Vertical Layout for this specific test
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4; // Ensure the indent matches our expected string

        // Act
        var result = db.Query<OrderModel>(o =>
        {
            IEnumerable<ISqlOrderFragment> sorts = 
            [
                o.OrderBy("Total"),
                o.OrderBy(x => x.Id, SqlOrderDirection.Desc)
            ];

            db.Append($$"""
                SELECT *
                FROM {{o}}
                ORDER BY {{sorts}}
                """);
        }).Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(SelectEntityExpansionVerticalLayoutData))]
    public void Select_EntityExpansion_VerticalLayout(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;

        // Act
        var result = db.Query<ProductWithIgnoreModel>(p => db.Append($$"""
            SELECT {{p}}
            FROM {{p}} AS p1
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> Select_WithNewLinesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT 
                    <<dbo>>.<<Products>>.<<Id>>, 
                    <<dbo>>.<<Products>>.<<PROD_NAME>>
                FROM 
                    <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT 
                    `dbo`.`Products`.`Id`, 
                    `dbo`.`Products`.`PROD_NAME`
                FROM 
                    `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT 
                    "dbo"."Products"."Id", 
                    "dbo"."Products"."PROD_NAME"
                FROM 
                    "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT 
                    "dbo"."Products"."Id", 
                    "dbo"."Products"."PROD_NAME"
                FROM 
                    "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT 
                    "dbo"."Products"."Id", 
                    "dbo"."Products"."PROD_NAME"
                FROM 
                    "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT 
                    [dbo].[Products].[Id], 
                    [dbo].[Products].[PROD_NAME]
                FROM 
                    [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> Select_WithTabsData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT	<<dbo>>.<<Products>>.<<Id>>,	<<dbo>>.<<Products>>.<<PROD_NAME>>
                FROM	<<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT	`dbo`.`Products`.`Id`,	`dbo`.`Products`.`PROD_NAME`
                FROM	`dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT	"dbo"."Products"."Id",	"dbo"."Products"."PROD_NAME"
                FROM	"dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT	"dbo"."Products"."Id",	"dbo"."Products"."PROD_NAME"
                FROM	"dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT	"dbo"."Products"."Id",	"dbo"."Products"."PROD_NAME"
                FROM	"dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT	[dbo].[Products].[Id],	[dbo].[Products].[PROD_NAME]
                FROM	[dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> Select_WithExtraSpacesData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>
                  FROM <<dbo>>.<<Products>>
                 WHERE <<dbo>>.<<Products>>.<<Id>> = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id`
                  FROM `dbo`.`Products`
                 WHERE `dbo`.`Products`.`Id` = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id"
                  FROM "dbo"."Products"
                 WHERE "dbo"."Products"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id"
                  FROM "dbo"."Products"
                 WHERE "dbo"."Products"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id"
                  FROM "dbo"."Products"
                 WHERE "dbo"."Products"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id]
                  FROM [dbo].[Products]
                 WHERE [dbo].[Products].[Id] = 1
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> Select_WithMixedWhitespaceData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """

                SELECT <<dbo>>.<<Products>>.<<Id>>
                FROM <<dbo>>.<<Products>>

                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """

                SELECT `dbo`.`Products`.`Id`
                FROM `dbo`.`Products`

                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """

                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"

                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """

                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"

                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """

                SELECT "dbo"."Products"."Id"
                FROM "dbo"."Products"

                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """

                SELECT [dbo].[Products].[Id]
                FROM [dbo].[Products]

                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> Select_WithCommentsData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>> -- This is the primary key
                FROM <<dbo>>.<<Products>> /* This is the table */
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT `dbo`.`Products`.`Id` -- This is the primary key
                FROM `dbo`.`Products` /* This is the table */
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT "dbo"."Products"."Id" -- This is the primary key
                FROM "dbo"."Products" /* This is the table */
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT "dbo"."Products"."Id" -- This is the primary key
                FROM "dbo"."Products" /* This is the table */
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id" -- This is the primary key
                FROM "dbo"."Products" /* This is the table */
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT [dbo].[Products].[Id] -- This is the primary key
                FROM [dbo].[Products] /* This is the table */
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> InsertVerticalLayoutData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                $$"""
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
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                $$"""
                INSERT INTO `dbo`.`Orders`
                (
                    `order_status`,
                    `Total`
                )
                VALUES
                (
                    @p0,
                    @p1
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                $$"""
                INSERT INTO "dbo"."Orders"
                (
                    "order_status",
                    "Total"
                )
                VALUES
                (
                    :0,
                    :1
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                $$"""
                INSERT INTO "dbo"."Orders"
                (
                    "order_status",
                    "Total"
                )
                VALUES
                (
                    $1,
                    $2
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                $$"""
                INSERT INTO "dbo"."Orders"
                (
                    "order_status",
                    "Total"
                )
                VALUES
                (
                    ?0,
                    ?1
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                $$"""
                INSERT INTO [dbo].[Orders]
                (
                    [order_status],
                    [Total]
                )
                VALUES
                (
                    @p0,
                    @p1
                )
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> UpdateVerticalLayoutData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                $$"""
                UPDATE <<dbo>>.<<Orders>>
                SET
                    <<order_status>> = !!100,
                    <<Total>> = !!101
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                $$"""
                UPDATE `dbo`.`Orders`
                SET
                    `order_status` = @p0,
                    `Total` = @p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                $$"""
                UPDATE "dbo"."Orders"
                SET
                    "order_status" = :0,
                    "Total" = :1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                $$"""
                UPDATE "dbo"."Orders"
                SET
                    "order_status" = $1,
                    "Total" = $2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                $$"""
                UPDATE "dbo"."Orders"
                SET
                    "order_status" = ?0,
                    "Total" = ?1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                $$"""
                UPDATE [dbo].[Orders]
                SET
                    [order_status] = @p0,
                    [Total] = @p1
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> BulkInsertVerticalLayoutData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                $$"""
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
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Products`
                (
                    `PROD_NAME`,
                    `CategoryId`,
                    `Price`
                )
                VALUES
                (
                    @p0,
                    @p1,
                    @p2
                ),
                (
                    @p3,
                    @p4,
                    @p5
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                INSERT INTO "dbo"."Products"
                (
                    "PROD_NAME",
                    "CategoryId",
                    "Price"
                )
                VALUES
                (
                    :0,
                    :1,
                    :2
                ),
                (
                    :3,
                    :4,
                    :5
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                INSERT INTO "dbo"."Products"
                (
                    "PROD_NAME",
                    "CategoryId",
                    "Price"
                )
                VALUES
                (
                    $1,
                    $2,
                    $3
                ),
                (
                    $4,
                    $5,
                    $6
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                INSERT INTO "dbo"."Products"
                (
                    "PROD_NAME",
                    "CategoryId",
                    "Price"
                )
                VALUES
                (
                    ?0,
                    ?1,
                    ?2
                ),
                (
                    ?3,
                    ?4,
                    ?5
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                INSERT INTO [dbo].[Products]
                (
                    [PROD_NAME],
                    [CategoryId],
                    [Price]
                )
                VALUES
                (
                    @p0,
                    @p1,
                    @p2
                ),
                (
                    @p3,
                    @p4,
                    @p5
                )
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> WhereInVerticalLayoutData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                $$"""
                SELECT *
                FROM <<dbo>>.<<Orders>>
                WHERE <<dbo>>.<<Orders>>.<<Id>> IN (
                    !!100,
                    !!101,
                    !!102
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                $$"""
                SELECT *
                FROM `dbo`.`Orders`
                WHERE `dbo`.`Orders`.`Id` IN (
                    @p0,
                    @p1,
                    @p2
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                $$"""
                SELECT *
                FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" IN (
                    :0,
                    :1,
                    :2
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                $$"""
                SELECT *
                FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" IN (
                    $1,
                    $2,
                    $3
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                $$"""
                SELECT *
                FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" IN (
                    ?0,
                    ?1,
                    ?2
                )
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                $$"""
                SELECT *
                FROM [dbo].[Orders]
                WHERE [dbo].[Orders].[Id] IN (
                    @p0,
                    @p1,
                    @p2
                )
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> OrderByEnumerableVerticalLayoutData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT *
                FROM <<dbo>>.<<Orders>>
                ORDER BY 
                    <<dbo>>.<<Orders>>.<<Total>>,
                    <<dbo>>.<<Orders>>.<<Id>> DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT *
                FROM `dbo`.`Orders`
                ORDER BY 
                    `dbo`.`Orders`.`Total`,
                    `dbo`.`Orders`.`Id` DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY 
                    "dbo"."Orders"."Total",
                    "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY 
                    "dbo"."Orders"."Total",
                    "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY 
                    "dbo"."Orders"."Total",
                    "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            [
                """
                SELECT *
                FROM [dbo].[Orders]
                ORDER BY 
                    [dbo].[Orders].[Total],
                    [dbo].[Orders].[Id] DESC
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> SelectEntityExpansionVerticalLayoutData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    p1.<<Id>>,
                    p1.<<PROD_NAME>>
                FROM <<dbo>>.<<Products>> AS p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT
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
                SELECT
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
                SELECT
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
                SELECT
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
                SELECT
                    p1.[Id],
                    p1.[PROD_NAME]
                FROM [dbo].[Products] AS p1
                """
            ]
        )
    ];
}