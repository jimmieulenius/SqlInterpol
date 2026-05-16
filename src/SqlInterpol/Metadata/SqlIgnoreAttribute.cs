using System;

namespace SqlInterpol.Metadata;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SqlIgnoreAttribute : Attribute
{
}