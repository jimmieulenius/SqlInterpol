using SqlInterpol.Metadata;

namespace SqlInterpol;

public static class SqlBuilderExtensions
{
    public static (SqlBuilder Builder, SqlEntity<T1> T1) 
        Entities<T1>(this SqlBuilder b) 
        => (b, b.CreateEntity<T1>());

    public static (SqlBuilder Builder, SqlEntity<T1> T1, SqlEntity<T2> T2) 
        Entities<T1, T2>(this SqlBuilder b) 
        => (b, b.CreateEntity<T1>(), b.CreateEntity<T2>());

    public static (SqlBuilder Builder, SqlEntity<T1> T1, SqlEntity<T2> T2, SqlEntity<T3> T3) 
        Entities<T1, T2, T3>(this SqlBuilder b) 
        => (b, b.CreateEntity<T1>(), b.CreateEntity<T2>(), b.CreateEntity<T3>());
}