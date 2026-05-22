namespace SqlInterpol;

/// <summary>
/// Overrides the global <see cref="SqlInterpolOptions.EnumFormat"/> for a specific property,
/// controlling whether the enum is serialized as an integer or its string name.
/// </summary>
/// <param name="format">The <see cref="SqlEnumFormat"/> to apply to this property.</param>
[AttributeUsage(AttributeTargets.Property)]
public class SqlEnumFormatAttribute(SqlEnumFormat format) : Attribute
{
    /// <summary>Gets the enum format to use for this property.</summary>
    public SqlEnumFormat Format { get; } = format;
}