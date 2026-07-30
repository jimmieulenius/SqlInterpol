// using SqlInterpol.Test.Models;

// namespace SqlInterpol.Test;

// public abstract class LockSuiteBase
// {
//     [Theory]
//     [MemberData(nameof(SelectWithForUpdateData))]
//     public void Select_WithForUpdate(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         int id = 5;

//         // Act - Zero-allocation properties and fluent target tracking
//         testCase.Action(() => 
//         {
//             db.Entity<Product>(out var p);
//             return db.Append($$"""
//                 SELECT {{p.Id}}, {{p.Name}}
//                 FROM {{p}} FOR UPDATE
//                 WHERE {{p.Id}} = {{id}}
//                 """).Build();
//         });

//         // Assert - Handles both string verification and exception assertions natively
//         testCase.Assert();

//         if (SelectWithForUpdateTheory?.AotIntercepted ?? false)
//         {
//             db.AssertAotIntercepted();
//         }
//     }

//     [Theory]
//     [MemberData(nameof(SelectWithForShareData))]
//     public void Select_WithForShare(SqlTestCase testCase)
//     {
//         // Arrange
//         var db = testCase.CreateBuilder();
//         int id = 5;

//         // Act
//         testCase.Action(() => 
//         {
//             db.Entity<Product>(out var p);
//             return db.Append($$"""
//                 SELECT {{p.Id}}, {{p.Name}}
//                 FROM {{p}} FOR SHARE
//                 WHERE {{p.Id}} = {{id}}
//                 """).Build();
//         });

//         // Assert
//         testCase.Assert();

//         if (SelectWithForShareTheory?.AotIntercepted ?? false)
//         {
//             db.AssertAotIntercepted();
//         }
//     }

//     public static SqlTheory? SelectWithForUpdateTheory { get; set; }

//     public static TheoryData<SqlTestCase>? SelectWithForUpdateData => SelectWithForUpdateTheory?.Data;

//     public static SqlTheory? SelectWithForShareTheory { get; set; }

//     public static TheoryData<SqlTestCase>? SelectWithForShareData => SelectWithForShareTheory?.Data;
// }