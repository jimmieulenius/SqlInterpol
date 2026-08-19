using System.Reflection;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class UpdateTestSuite
{
    public class DummyEntityProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => null;
    }
}