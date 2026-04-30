using SqlInterpol.Metadata;

namespace SqlInterpol;

public static partial class SqlBuilderExtensions
{
    extension (SqlBuilder builder)
    {
        public (SqlBuilder Builder, ISqlEntity<T1> T1) Entities<T1>() =>
            (builder, builder.CreateEntity<T1>());

        public (SqlBuilder Builder, ISqlEntity<T1> T1, ISqlEntity<T2> T2) Entities<T1, T2>() =>
            (builder, builder.CreateEntity<T1>(), builder.CreateEntity<T2>());

        public (SqlBuilder Builder, ISqlEntity<T1> T1, ISqlEntity<T2> T2, ISqlEntity<T3> T3) Entities<T1, T2, T3>() =>
            (builder, builder.CreateEntity<T1>(), builder.CreateEntity<T2>(), builder.CreateEntity<T3>());
    }
}