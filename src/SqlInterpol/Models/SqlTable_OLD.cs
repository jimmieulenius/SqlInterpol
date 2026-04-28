// using SqlInterpol.Constants;

// namespace SqlInterpol.Models;

// public class SqlTable(string name, string? schema = null, string? alias = null) : SqlReference(name, alias)
// {
//     private string? _schema = schema;

//     public SqlTable(SqlTable table) : this(table.Name, table.Schema(), table.Alias())
//     {
//     }

//     public string? Schema() => _schema;

//     public string? Schema(string schema)
//     {
//         _schema = schema;

//         return _schema;
//     }

//     public override string FullName => $"{(Schema() != null ? $"[{Schema()}]." : null)}[{Name}]";

//     public override string Reference => Alias() != null ? $"[{Alias()}]" : FullName;

//     public override string ToString(string clause)
//     {
//         return ToString(clause, Sql.CurrentOptions);
//     }

//     public override string ToString(string clause, SqlInterpolOptions options)
//     {
//         return ToString(clause, options, isInAsContext: false);
//     }

//     public override string ToString(string clause, SqlInterpolOptions options, bool isInAsContext)
//     {
//         try
//         {
//             IsAsAlias = isInAsContext;  // Set for this occurrence
            
//             var start = options.IdentifierStart;
//             var end = options.IdentifierEnd;
//             var fullName = $"{(Schema() != null ? $"{start}{Schema()}{end}." : null)}{start}{Name}{end}";
            
//             // If this table is in an "AS alias" context, only render the full name without the alias part
//             // The " AS alias" will come from the raw SQL template and Alias() return value
//             if (IsAsAlias && clause == SqlKeyword.From)
//             {
//                 return fullName;
//             }
            
//             return Alias() != null ? $"{fullName} AS {start}{Alias()}{end}" : fullName;
//         }
//         finally
//         {
//             IsAsAlias = false;  // Always reset after rendering
//         }
//     }

//     public override SqlReference As(string alias)
//     {
//         _alias = alias;
//         return this;
//     }
// }