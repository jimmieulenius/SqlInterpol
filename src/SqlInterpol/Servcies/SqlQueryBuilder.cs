// using System.Runtime.CompilerServices;
// using SqlInterpol.Handlers;
// using SqlInterpol.Models;

// namespace SqlInterpol.Services;

// public class SqlQueryBuilder
// {
//     public static SqlQuery Build([InterpolatedStringHandlerArgument] SqlQueryInterpolatedStringHandler handler)
//     {
//         var options = Sql.CurrentOptions;

//         Sql.SetCurrentOptions(options);
        
//         try
//         {
//             return handler.ToQuery(options);
//         }
//         finally
//         {
//             Sql.SetCurrentOptions(null);
//         }
//     }
// }