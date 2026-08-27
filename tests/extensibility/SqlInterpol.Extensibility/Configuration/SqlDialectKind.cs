#if !CSHARP14_EXTENSION_TYPES
namespace SqlInterpol.Extensibility.Dialects;

public static class SqlDialectKind
{
    public static readonly SqlInterpol.Dialects.SqlDialectKind CustomDb = new("CustomDb");
}
#endif