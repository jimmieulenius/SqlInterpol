using System.Collections;
using System.Text;

namespace SqlInterpol;

/// <summary>
/// A high-performance batch SQL template (e.g. Batched Updates or Deletes) that loops over a collection,
/// appending a complete SQL statement per row separated by semicolons.
/// </summary>
public class SqlBatchTemplate : ISqlTemplate
{
    private readonly string _statementFormat;
    private readonly Func<object, object?[]> _rowExtractor;

    public SqlBatchTemplate(string statementFormat, Func<object, object?[]> rowExtractor)
    {
        _statementFormat = statementFormat;
        _rowExtractor = rowExtractor;
    }

    /// <inheritdoc />
    public string Render(ISqlContext context, object? arguments = null)
    {
        if (arguments is not IEnumerable items || arguments is string) return string.Empty;

        var sb = new StringBuilder();
        bool first = true;

        foreach (object item in items)
        {
            // FIX: Explicitly use \n instead of Environment.NewLine (AppendLine) to match raw string literal rules
            if (!first) sb.Append(";\n");
            
            var vals = _rowExtractor(item);
            var paramNames = new string[vals.Length];
            for (int i = 0; i < vals.Length; i++) paramNames[i] = context.AddParameter(vals[i]);
            
            sb.AppendFormat(_statementFormat, (object[])paramNames);
            first = false;
        }

        return sb.ToString();
    }
}