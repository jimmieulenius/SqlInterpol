namespace SqlInterpol;

/// <summary>
/// Controls how enum values are serialized as SQL parameters.
/// </summary>
/// <seealso cref="SqlInterpolOptions.EnumFormat"/>
/// <seealso cref="SqlEnumFormatAttribute"/>
public enum SqlEnumFormat
{
    /// <summary>Stores the enum's underlying integer value (default).</summary>
    Integer,

    /// <summary>Stores the enum's string name as returned by <c>Enum.GetName</c>.</summary>
    String
}