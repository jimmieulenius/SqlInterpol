namespace SqlInterpol;

public interface ISqlContext
{
    ISqlDialect Dialect { get; }
    SqlInterpolOptions Options { get; }
    IDictionary<string, object?> Parameters { get; }

    string AddParameter(object? value);
}