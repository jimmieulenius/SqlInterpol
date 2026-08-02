#if !CSHARP14_EXTENSION_TYPES
namespace SqlInterpol.Extensibility.Configuration;

public static class SqlDialectKind
{
    public static readonly SqlInterpol.Configuration.SqlDialectKind CustomDb = new("CustomDb");
}
#endif