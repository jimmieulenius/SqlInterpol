
namespace SqlInterpol;

public class SqlOrderFragment : ISqlOrderFragment
{
    private readonly ISqlReference? _reference;
    private readonly ISqlEntityBase? _entity;
    private readonly string? _physicalColumnName;
    private readonly SqlOrderDirection? _direction;

    public SqlOrderFragment(ISqlReference reference, SqlOrderDirection? direction = null)
    {
        _reference = reference;
        _direction = direction;
    }

    public SqlOrderFragment(ISqlEntityBase entity, string physicalColumnName, SqlOrderDirection? direction = null)
    {
        _entity = entity;
        _physicalColumnName = physicalColumnName;
        _direction = direction;
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        string columnSql;

        if (_reference != null)
        {
            columnSql = _reference.ToSql(context, mode);
        }
        else if (_entity != null && _physicalColumnName != null)
        {
            string sourcePointer = _entity.Reference.ToSql(context, SqlRenderMode.AliasOnly);
            
            if (string.IsNullOrWhiteSpace(sourcePointer))
            {
                sourcePointer = _entity.Reference.ToSql(context, SqlRenderMode.BaseName);
            }

            string quotedColumn = context.Dialect.QuoteIdentifier(_physicalColumnName);
            columnSql = $"{sourcePointer}.{quotedColumn}";
        }
        else
        {
            throw new InvalidOperationException("Invalid SqlOrderFragment configuration.");
        }

        string dirSql = _direction switch
        {
            SqlOrderDirection.Desc => $" {SqlKeyword.Desc.Value}",
            SqlOrderDirection.Asc => $" {SqlKeyword.Asc.Value}",
            _ => string.Empty
        };
            
        return $"{columnSql}{dirSql}";
    }
}