namespace SqlInterpol;

/// <summary>
/// Represents a strongly-typed template marker for expanding <typeparamref name="TDto"/> into SQL fragments.
/// </summary>
/// <typeparam name="TDto">The type of the data transfer object to expand.</typeparam>
public readonly struct SqlExpandable<TDto> : ISqlExpandable
{
    /// <inheritdoc />
    public Type DtoType => typeof(TDto);

    /// <inheritdoc />
    public IReadOnlySet<string> KeyProperties { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlExpandable{TDto}"/> struct.
    /// </summary>
    /// <param name="keys">An optional array of property names to treat as keys during parser expansion.</param>
    public SqlExpandable(string[]? keys)
    {
        KeyProperties = keys != null && keys.Length > 0 
            ? new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase) 
            : [];
    }
}