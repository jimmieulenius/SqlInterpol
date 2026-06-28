// // TEMPORARY: verify SQLI004 fires on typo, then delete
// using SqlInterpol;
// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// internal class _SqliTypoTest
// {
//     internal void TestTypo()
//     {
//         var db = SqlBuilder.PostgreSql();
//         // Deliberate typo SELCT — must produce SQLI004 warning
//         var result = db.Query<Product>(p =>
//             db.Append($"SELCT {p[x => x.Id]} FROM {p}")).Build();
//     }
// }
