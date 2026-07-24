using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class SqlBuilderTests
{
    [Theory]
    [MemberData(nameof(AppendData))]
    public void SqlBuilder_Append(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($"SELECT {p.Id}").Append($" FROM {p}")
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(AppendLineData))]
    public void SqlBuilder_AppendLine(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .AppendLine($"SELECT {p.Id}")
            .Append($"FROM {p}")
            .Build()
        );
    
        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(RawStringData))]
    public void SqlBuilder_RawString(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT
                {{p.Id}}
            FROM {{p}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    // --- TEST DATA ---

    public static TheoryData<SqlTestCase> AppendData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                "SELECT <<dbo>>.<<Products>>.<<Id>> FROM <<dbo>>.<<Products>>"
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                "SELECT `dbo`.`Products`.`Id` FROM `dbo`.`Products`"
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\""
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\""
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                "SELECT \"dbo\".\"Products\".\"Id\" FROM \"dbo\".\"Products\""
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                "SELECT [dbo].[Products].[Id] FROM [dbo].[Products]"
            ]
        )
    ];

    public static TheoryData<SqlTestCase> AppendLineData => 
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                $"SELECT <<dbo>>.<<Products>>.<<Id>>{Environment.NewLine}FROM <<dbo>>.<<Products>>"
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                $"SELECT `dbo`.`Products`.`Id`{Environment.NewLine}FROM `dbo`.`Products`"
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine}FROM \"dbo\".\"Products\""
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine}FROM \"dbo\".\"Products\""
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                $"SELECT \"dbo\".\"Products\".\"Id\"{Environment.NewLine}FROM \"dbo\".\"Products\""
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                $"SELECT [dbo].[Products].[Id]{Environment.NewLine}FROM [dbo].[Products]"
            ]
        )
    ];

    public static TheoryData<SqlTestCase> RawStringData =>
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
}