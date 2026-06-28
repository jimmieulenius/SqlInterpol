// using SqlInterpol.Test.Dialects;
// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// public class DialectTests
// {
//     [Theory]
//     [MemberData(nameof(DialectErrorData))]
//     public void Dialect_ValidationRules(SqlErrorTestCase testCase)
//     {
//         // Act
//         var exception = Record.Exception(() => {
//             var db = testCase.CreateBuilder();
//             db.Context.Dialect.RenderFragment(new UnsupportedDummyFragment(), db.Context);
//         });

//         // Assert
//         testCase.AssertException(exception);
//     }

//     public static TheoryData<SqlErrorTestCase> DialectErrorData =>
//     [
//         new SqlErrorTestCase(
//             SqlDialectKind.CustomDb,
//             typeof(NotSupportedException),
//             "The fragment type 'UnsupportedDummyFragment' is not supported by CustomDbSqlDialect."
//         )
//     ];
// }