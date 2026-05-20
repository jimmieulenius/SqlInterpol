using System;

namespace SqlInterpol;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SqlIgnoreAttribute : Attribute
{
}