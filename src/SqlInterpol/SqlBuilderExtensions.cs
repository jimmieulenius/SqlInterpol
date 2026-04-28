using SqlInterpol.Metadata;
using SqlInterpol.Parsing;

namespace SqlInterpol;

public static class SqlBuilderExtensions
{
    public static SqlQueryResult Append<T>(
        this SqlBuilder builder, 
        Func<SqlTable<T>, SqlQueryInterpolatedStringHandler> queryFunc)
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        var table = new SqlTable<T>(meta.Name, meta.Schema);

        // This invokes the InterpolatedStringHandler logic
        var handler = queryFunc(table);

        return builder.ExecuteHandler(handler);
    }

    public static SqlQueryResult Append<T1, T2>(
        this SqlBuilder builder, 
        Func<SqlTable<T1>, SqlTable<T2>, SqlQueryInterpolatedStringHandler> queryFunc)
    {
        var m1 = SqlMetadataRegistry.GetMetadata<T1>();
        var m2 = SqlMetadataRegistry.GetMetadata<T2>();

        var t1 = new SqlTable<T1>(m1.Name, m1.Schema);
        var t2 = new SqlTable<T2>(m2.Name, m2.Schema);

        var handler = queryFunc(t1, t2);

        return builder.ExecuteHandler(handler);
    }
}