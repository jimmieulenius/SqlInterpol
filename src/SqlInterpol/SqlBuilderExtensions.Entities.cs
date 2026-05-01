namespace SqlInterpol;

public static partial class SqlBuilderExtensions
{
    extension (SqlBuilder builder)
    {
        public ISqlEntity<T> Entity<T>(string? name = null, string? schema = null)
            => ((ISqlEntityRegistry)builder).RegisterEntity<T>(name, schema);

        public SqlBuilder With<T1, T2>(
            Action<SqlBuilder, ISqlEntity<T1>, ISqlEntity<T2>> body,
            string? name1 = null, string? name2 = null)
        {
            var e1 = ((ISqlEntityRegistry)builder).RegisterEntity<T1>(name1);
            var e2 = ((ISqlEntityRegistry)builder).RegisterEntity<T2>(name2);
            body(builder, e1, e2);

            return builder;
        }
    }
}