// namespace SqlInterpol.Models;

// internal class SqlFormat : SqlEmbedded
// {
//     public string Clause { get; }
//     public string? Template { get; }
//     public object?[]? Args { get; }

//     public SqlFormat(string value, Dictionary<string, object?> parameters, string clause) 
//         : base(value, parameters)
//     {
//         Clause = clause;
//         // Template and Args remain null - old behavior for backward compatibility
//         Template = null;
//         Args = null;
//     }

//     public SqlFormat(string value, Dictionary<string, object?> parameters, string clause, string template, object?[] args)
//         : base(value, parameters)
//     {
//         Clause = clause;
//         Template = template;
//         Args = args;
//     }
// }