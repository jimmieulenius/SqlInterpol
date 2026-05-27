namespace SqlInterpol;

/// <summary>
/// Represents a single ORDER BY expression, rendering a column reference optionally
/// followed by <c>ASC</c> or <c>DESC</c>.
/// </summary>
public class SqlOrderFragment : ISqlOrderFragment, ISqlSwappableFragment
{
    private readonly ISqlReference? _reference;
    private readonly ISqlEntityBase? _entity;
    private readonly string? _physicalColumnName;
    private readonly SqlOrderDirection? _direction;

    /// <summary>
    /// Initializes an order fragment from a typed column reference.
    /// </summary>
    /// <param name="reference">The column reference to sort by.</param>
    /// <param name="direction">The sort direction, or <see langword="null"/> for no keyword.</param>
    public SqlOrderFragment(ISqlReference reference, SqlOrderDirection? direction = null)
    {
        _reference = reference;
        _direction = direction;
    }

    /// <summary>
    /// Initializes an order fragment from a raw physical column name on an entity.
    /// </summary>
    /// <param name="entity">The entity that provides the table/alias prefix.</param>
    /// <param name="physicalColumnName">The physical column name to sort by.</param>
    /// <param name="direction">The sort direction, or <see langword="null"/> for no keyword.</param>
    public SqlOrderFragment(ISqlEntityBase entity, string physicalColumnName, SqlOrderDirection? direction = null)
    {
        _entity = entity;
        _physicalColumnName = physicalColumnName;
        _direction = direction;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public ISqlFragment Swap(
        Dictionary<ISqlReference, ISqlEntityBase> entityMap, 
        IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters, 
        object? arguments)
    {
        if (_reference != null)
        {
            ISqlReference mappedRef = _reference;
            if (_reference is SqlColumnReference colRef && entityMap.TryGetValue(colRef.SourceReference, out var realEntity))
            {
                mappedRef = new SqlColumnReference(realEntity.Reference, colRef.ColumnName, colRef.PropertyName);
            }
            return new SqlOrderFragment(mappedRef, _direction);
        }

        if (_entity != null && _physicalColumnName != null)
        {
            ISqlEntityBase mappedEntity = _entity;
            if (entityMap.TryGetValue(_entity.Reference, out var realEntity))
            {
                mappedEntity = realEntity;
            }
            return new SqlOrderFragment(mappedEntity, _physicalColumnName, _direction);
        }

        return this;
    }
}