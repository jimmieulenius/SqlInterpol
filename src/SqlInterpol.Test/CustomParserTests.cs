// using SqlInterpol.Config;
// using SqlInterpol.Parsing;

// namespace SqlInterpol.Tests;

// public class CustomParserTests
// {
// // A custom parser that prefixes every literal to prove it's being used
//     private class PrefixingParser : DefaultSqlParser
//     {
//         public override void ProcessLiteral(SqlContext context, ReadOnlySpan<char> span)
//         {
//             var prefixed = "/* CUSTOM */ " + span.ToString();
//             base.ProcessLiteral(context, prefixed.AsSpan());
//         }
//     }

//     [Theory]
//     [MemberData(nameof(CustomParserData))]
//     public void Builder_Uses_Custom_Parser_From_Options(string _, SqlBuilder db, string expected)
//     {
//         // Act
//         db.Append("SELECT 1");
//         var result = db.Build();

//         // Assert
//         Assert.StartsWith("/* CUSTOM */", result.Sql);
//         Assert.Equal(expected, result.Sql);
//     }

//     public static TheoryData<string, SqlBuilder, string> CustomParserData()
//     {
//         var data = new TheoryData<string, SqlBuilder, string>();
        
//         // Define options with our custom parser
//         var options = new SqlInterpolOptions { Parser = new PrefixingParser() };

//         // We create builders manually using the dialect and our options
//         data.Add(
//             SqlDialectKind.SqlServer.ToString(),
//             new SqlBuilder(new SqlServerSqlDialect(), options),
//             "/* CUSTOM */ SELECT 1"
//         );

//         data.Add(
//             SqlDialectKind.CustomDb.ToString(),
//             new SqlBuilder(new CustomDbDialect(), options),
//             "/* CUSTOM */ SELECT 1"
//         );

//         return data;
//     }
// }