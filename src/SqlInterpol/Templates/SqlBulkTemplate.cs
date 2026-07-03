using System.Text;

namespace SqlInterpol;

/// <summary>
/// A high-performance bulk SQL template (e.g. Bulk Inserts) that loops over a collection,
/// dynamically adapting layout formatting based on context configuration options to mirror SqlInsertValuesFragment.
/// </summary>
public class SqlBulkTemplate : ISqlTemplate
{
    private readonly string _tableName;
    private readonly IReadOnlyList<string> _columnNames;
    private readonly string _rowFormat;
    private readonly Func<object, object?[]> _rowExtractor;
    private readonly string? _customSeparator;

    public SqlBulkTemplate(string tableName, IReadOnlyList<string> columnNames, string rowFormat, Func<object, object?[]> rowExtractor, string? customSeparator = null)
    {
        _tableName = tableName;
        _columnNames = columnNames;
        _rowFormat = rowFormat;
        _rowExtractor = rowExtractor;
        _customSeparator = customSeparator;
    }

    /// <inheritdoc />
    public string Render(ISqlContext context, object? arguments = null)
    {
        var columnsText = string.Join(", ", _columnNames);

        if (arguments is not System.Collections.IEnumerable items || arguments is string) 
        {
            return $"INSERT INTO {_tableName} ({columnsText}){Environment.NewLine}{SqlKeyword.Values}";
        }

        var baseSeparator = _customSeparator ?? context.Options.CollectionSeparator; // Defaults to ", "
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
                
                for (int i = 0; i < vals.Length; i++) paramNames[i] = context.AddParameter(vals[i]);
                
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
                
                for (int i = 0; i < vals.Length; i++) paramNames[i] = context.AddParameter(vals[i]);
                
                sb.AppendFormat(_rowFormat, (object[])paramNames);
                first = false;
            }
        }

        return sb.ToString();
    }
}