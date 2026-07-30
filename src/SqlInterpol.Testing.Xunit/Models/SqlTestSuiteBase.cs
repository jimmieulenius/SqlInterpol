namespace SqlInterpol.Testing.Xunit;

public abstract class SqlTestSuiteBase<T>
{
    protected abstract SqlBuilder CreateBuilder();
}