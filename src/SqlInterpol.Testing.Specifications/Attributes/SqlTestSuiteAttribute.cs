using System;

namespace SqlInterpol.Testing.Specifications;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SqlTestSuiteAttribute(Type interfaceType) : Attribute
{
    public Type InterfaceType { get; } = interfaceType;
}