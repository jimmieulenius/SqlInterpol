namespace SqlInterpol.Testing.Specifications;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public sealed class SqlIgnoreMemberAttribute : Attribute
{
}