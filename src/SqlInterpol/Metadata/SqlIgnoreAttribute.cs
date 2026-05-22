namespace SqlInterpol;

/// <summary>
/// Excludes a property from column mapping and DTO property discovery.
/// Properties marked with this attribute are invisible to <see cref="SqlMetadataRegistry"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SqlIgnoreAttribute : Attribute
{
}