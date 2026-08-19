using System.Text;
using SqlInterpol.Configuration;

namespace SqlInterpol.Execution;

/// <summary>
/// A high-performance bulk SQL template (e.g. bulk INSERTs) that loops over a collection,
/// dynamically adapting layout formatting based on context configuration options.
/// </summary>
public class SqlBulkTemplate : ISqlTemplate
{
    private readonly string _insertPrefix;
    private readonly string _rowFormat;
    private readonly Func<object, object?[]> _rowExtractor;
    private readonly string? _customSeparator;

    /// <summary>
    /// Initializes a new <see cref="SqlBulkTemplate"/> from a pre-rendered INSERT prefix.
    /// </summary>
    /// <remarks>
    /// The <paramref name="insertPrefix"/> should contain everything up to (but not including)
    /// the <c>VALUES</c> keyword, e.g. <c>"INSERT INTO [Products] ([Id], [Name])"</c>.
    /// This constructor is used by the pipeline-based template cache so that dialect rewriters
    /// can customize the INSERT structure without modifying the cache.
    /// </remarks>
    /// <param name="insertPrefix">
    /// The pre-rendered INSERT prefix, e.g. <c>"INSERT INTO [Products] ([Id], [Name])"</c>.
    /// </param>
    /// <param name="rowFormat">
    /// The single-row values format string with positional holes, e.g. <c>"({0}, {1})"</c>.
    /// </param>
    /// <param name="rowExtractor">The delegate used to extract field values from each payload item.</param>
    /// <param name="customSeparator">An optional custom row separator override.</param>
    public SqlBulkTemplate(
        string insertPrefix,
        string rowFormat,
        Func<object, object?[]> rowExtractor,
        string? customSeparator = null)
    {
        _insertPrefix = insertPrefix;
        _rowFormat = rowFormat;
        _rowExtractor = rowExtractor;
        _customSeparator = customSeparator;
    }

    /// <summary>
    /// Initializes a new <see cref="SqlBulkTemplate"/> from explicit table name and column list.
    /// Provided for backward compatibility; delegates to the prefix-based constructor.
    /// </summary>
    /// <param name="tableName">The fully quoted target table name.</param>
    /// <param name="columnNames">The list of fully quoted column names being targeted.</param>
    /// <param name="rowFormat">The single-row values format string.</param>
    /// <param name="rowExtractor">The delegate used to extract field values from the payload item.</param>
    /// <param name="customSeparator">An optional custom row separator override.</param>
    public SqlBulkTemplate(
        string tableName,
        IReadOnlyList<string> columnNames,
        string rowFormat,
        Func<object, object?[]> rowExtractor,
        string? customSeparator = null)
        : this($"INSERT INTO {tableName} ({string.Join(", ", columnNames)})", rowFormat, rowExtractor, customSeparator) { }

    /// <inheritdoc />
    public string Render(ISqlContext context, object? arguments = null)
    {
        if (arguments is not System.Collections.IEnumerable items || arguments is string)
        {
            return $"{_insertPrefix}{Environment.NewLine}{SqlKeyword.Values}";
        }

        var baseSeparator = _customSeparator ?? context.Options.Value.CollectionSeparator;
        var sb = new StringBuilder();

        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            var separator = $"{baseSeparator.TrimEnd()}{Environment.NewLine}";
            sb.Append($"{_insertPrefix}{Environment.NewLine}{SqlKeyword.Values}{Environment.NewLine}");

            bool first = true;
            foreach (object item in items)
            {
                if (!first) sb.Append(separator);
                var vals = _rowExtractor(item);
                var paramNames = new string[vals.Length];
                for (int i = 0; i < vals.Length; i++)
                    paramNames[i] = context.AddParameter(vals[i]);
                sb.Append('(');
                sb.AppendFormat(_rowFormat.Trim('(', ')'), (object[])paramNames);
                sb.Append(Environment.NewLine);
                sb.Append(')');
                first = false;
            }
        }
        else
        {
            var separator = baseSeparator;
            sb.Append($"{_insertPrefix}{Environment.NewLine}{SqlKeyword.Values} ");

            bool first = true;
            foreach (object item in items)
            {
                if (!first) sb.Append(separator);
                var vals = _rowExtractor(item);
                var paramNames = new string[vals.Length];
                for (int i = 0; i < vals.Length; i++)
                    paramNames[i] = context.AddParameter(vals[i]);
                sb.AppendFormat(_rowFormat, (object[])paramNames);
                first = false;
            }
        }

        return sb.ToString();
    }
}