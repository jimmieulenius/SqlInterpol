namespace SqlInterpol.Schema;

/// <summary>
/// Specifies that a property should be entirely ignored by the SQL mapper and expansion macros.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public class SqlIgnoreAttribute : Attribute
{
}