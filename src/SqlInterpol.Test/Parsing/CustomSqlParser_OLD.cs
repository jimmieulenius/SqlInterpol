// using SqlInterpol.Parsing;
// using System.Collections;

// namespace SqlInterpol.Test.Parsing;

// public class CustomSqlParser : SqlInterpolationParser
// {
//     private bool _nextIsCollection;

//     public override string? ProcessLiteral(ISqlParserContext context, ReadOnlySpan<char> span)
//     {
//         // 1. Let the base parser handle all the standard keyword and alias tracking
//         base.ProcessLiteral(context, span);

//         // 2. Sniff for our custom "CUSTOM_IN" keyword
//         var trimmed = span.TrimEnd();

//         if (trimmed.EndsWith("CUSTOM_IN", StringComparison.OrdinalIgnoreCase))
//         {
//             _nextIsCollection = true;
//         }
//         else if (trimmed.Length > 0 && !char.IsWhiteSpace(span[^1]))
//         {
//             // Reset if the literal ends with something else
//             _nextIsCollection = false; 
//         }

//         return null;
//     }

//     public override SqlSegment ProcessValue(ISqlParserContext context, object? value)
//     {
//         // 3. Intercept if we saw "CUSTOM_IN" and the value is a list
//         if (_nextIsCollection && value is IEnumerable enumerable and not string)
//         {
//             _nextIsCollection = false; // consume the flag
            
//             // Return a Raw segment with our custom renderer
//             return new SqlSegment(SqlSegmentType.Raw, new SqlInListFragment(enumerable));
//         }

//         // 4. Otherwise, let the base class handle parameters, entities, etc.
//         return base.ProcessValue(context, value);
//     }
// }