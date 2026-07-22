using System.Text;
using SqlInterpol.Configuration;

namespace SqlInterpol.Execution;

/// <summary>
/// A high-performance bulk SQL template (e.g., Bulk Inserts) that loops over a collection,
/// dynamically adapting layout formatting based on context configuration options.
/// </summary>
/// <param name="tableName">The fully quoted target table name.</param>
/// <param name="columnNames">The list of fully quoted column names being targeted.</param>
/// <param name="rowFormat">The format string for a single values row.</param>
/// <param name="rowExtractor">The delegate used to extract field values from the payload item.</param>
/// <param name="customSeparator">An optional custom collection separator override.</param>
public class SqlBulkTemplate(
    string tableName, 
    IReadOnlyList<string> columnNames, 
    string rowFormat, 
    Func<object, object?[]> rowExtractor, 
    string? customSeparator = null) : ISqlTemplate
{
    private readonly string _tableName = tableName;
    private readonly IReadOnlyList<string> _columnNames = columnNames;
    private readonly string _rowFormat = rowFormat;
    private readonly Func<object, object?[]> _rowExtractor = rowExtractor;
    private readonly string? _customSeparator = customSeparator;

    /// <inheritdoc />
    public string Render(ISqlContext context, object? arguments = null)
    {
        var columnsText = string.Join(", ", _columnNames);
        if (arguments is not System.Collections.IEnumerable items || arguments is string) 
        {
            return $"INSERT INTO {_tableName} ({columnsText}){Environment.NewLine}{SqlKeyword.Values}";
        }
        
        var baseSeparator = _customSeparator ?? context.Options.CollectionSeparator;
        string separator;
        var sb = new StringBuilder();
        
        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            separator = $"{baseSeparator.TrimEnd()}{Environment.NewLine}";
            
            sb.Append($"INSERT INTO {_tableName} ({columnsText}{Environment.NewLine}){Environment.NewLine}{SqlKeyword.Values}{Environment.NewLine}");
            
            bool first = true;
            foreach (object item in items)
            {
                if (!first) sb.Append(separator);
                
                var vals = _rowExtractor(item);
                var paramNames = new string[vals.Length];
                
                for (int i = 0; i < vals.Length; i++) 
                {
                    paramNames[i] = context.AddParameter(vals[i]);
                }
                
                sb.Append('(');
                sb.AppendFormat(_rowFormat.Trim('(', ')'), (object[])paramNames);
                sb.Append(Environment.NewLine);
                sb.Append(')');
                first = false;
            }
        }
        else
        {
            separator = baseSeparator;
            sb.Append($"INSERT INTO {_tableName} ({columnsText}){Environment.NewLine}{SqlKeyword.Values} ");
            
            bool first = true;
            foreach (object item in items)
            {
                if (!first) sb.Append(separator);
                
                var vals = _rowExtractor(item);
                var paramNames = new string[vals.Length];
                
                for (int i = 0; i < vals.Length; i++) 
                {
                    paramNames[i] = context.AddParameter(vals[i]);
                }
                
                sb.AppendFormat(_rowFormat, (object[])paramNames);
                first = false;
            }
        }
        
        return sb.ToString();
    }
}