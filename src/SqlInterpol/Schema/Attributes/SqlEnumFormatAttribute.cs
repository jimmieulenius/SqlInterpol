using SqlInterpol.Configuration;

namespace SqlInterpol.Schema;

/// <summary>
/// Overrides the global <see cref="SqlEnumFormat"/> setting for a specific enum property.
/// </summary>
/// <param name="format">The format (Integer or String) to use when parameterizing this enum.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public class SqlEnumFormatAttribute(SqlEnumFormat format) : Attribute
{
    /// <summary>Gets the specific format applied to this enum property.</summary>
    public SqlEnumFormat Format { get; } = format;
}