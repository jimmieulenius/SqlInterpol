#if CSHARP14_EXTENSION_TYPES
using SqlInterpol.Configuration;

namespace SqlInterpol.Extensibility.Configuration;

public static class SqlDialectKindExtensions
{
    extension (SqlDialectKind _) 
    {
        public static SqlDialectKind CustomDb => new("CustomDb");
    }
}
#endif