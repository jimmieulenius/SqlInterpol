using System.Collections.Generic;

namespace SqlInterpol.Config;

public interface ISqlContext
{
    ISqlDialect Dialect { get; }
    SqlInterpolOptions Options { get; }
    IDictionary<string, object?> Parameters { get; } 
}