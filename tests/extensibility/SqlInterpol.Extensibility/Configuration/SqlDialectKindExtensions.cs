#if CSHARP14_EXTENSION_TYPES
using SqlInterpol.Dialects;

namespace SqlInterpol.Extensibility.Dialects;

public static class SqlDialectKindExtensions
{
    extension (SqlDialectKind _) 
    {
        public static SqlDialectKind CustomDb => new("CustomDb");
    }
}
#endif