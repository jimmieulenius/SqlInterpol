namespace SqlInterpol;

/// <summary>
/// Controls how enum values are serialized as SQL parameters.[cite: 3]
/// </summary>
/// <seealso cref="SqlInterpolOptions.EnumFormat"/>[cite: 3]
/// <seealso cref="SqlEnumFormatAttribute"/>[cite: 3]
public enum SqlEnumFormat
{
    /// <summary>Stores the enum's underlying integer value (default).</summary>[cite: 3]
    Integer,

    /// <summary>Stores the enum's string name as returned by <c>Enum.GetName</c>.</summary>[cite: 3]
    String
}