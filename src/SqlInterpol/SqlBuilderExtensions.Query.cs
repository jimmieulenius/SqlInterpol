namespace SqlInterpol;

public readonly record struct SqlEntityConfig(string? Name = null, string? Schema = null);

public static partial class SqlBuilderExtensions
{
    extension (SqlBuilder builder)
    {
        public ISqlEntity<T> Entity<T>(string? name = null, string? schema = null)
            => ((ISqlEntityRegistry)builder).RegisterEntity<T>(name, schema);

        public SqlBuilder Query<T1>(
            Action<ISqlEntity<T1>> body,
            SqlEntityConfig e1 = default)
        {
            var entity1 = builder.Entity<T1>(e1.Name, e1.Schema);
            body(entity1);

            return builder;
        }

        public SqlBuilder Query<T1>(
            Action<SqlBuilder, ISqlEntity<T1>> body,
            SqlEntityConfig e1 = default)
        {
            var entity1 = builder.Entity<T1>(e1.Name, e1.Schema);
            body(builder, entity1);

            return builder;
        }

        public SqlBuilder Query<T1, T2>(
            Action<ISqlEntity<T1>, ISqlEntity<T2>> body,
            SqlEntityConfig e1 = default,
            SqlEntityConfig e2 = default)
        {
            var entity1 = builder.Entity<T1>(e1.Name, e1.Schema);
            var entity2 = builder.Entity<T2>(e2.Name, e2.Schema);
            body(entity1, entity2);

            return builder;
        }

        public SqlBuilder Query<T1, T2>(
            Action<SqlBuilder, ISqlEntity<T1>, ISqlEntity<T2>> body,
            SqlEntityConfig e1 = default,
            SqlEntityConfig e2 = default)
        {
            var entity1 = builder.Entity<T1>(e1.Name, e1.Schema);
            var entity2 = builder.Entity<T2>(e2.Name, e2.Schema);
            body(builder, entity1, entity2);

            return builder;
        }

        public SqlBuilder Query<T1, T2, T3>(
            Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>> body,
            SqlEntityConfig e1 = default,
            SqlEntityConfig e2 = default,
            SqlEntityConfig e3 = default)
        {
            var entity1 = builder.Entity<T1>(e1.Name, e1.Schema);
            var entity2 = builder.Entity<T2>(e2.Name, e2.Schema);
            var entity3 = builder.Entity<T3>(e3.Name, e3.Schema);
            body(entity1, entity2, entity3);

            return builder;
        }

            public SqlBuilder Query<T1, T2, T3>(
                Action<SqlBuilder, ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>> body,
                SqlEntityConfig e1 = default,
                SqlEntityConfig e2 = default,
                SqlEntityConfig e3 = default)
            {
                var entity1 = builder.Entity<T1>(e1.Name, e1.Schema);
                var entity2 = builder.Entity<T2>(e2.Name, e2.Schema);
                var entity3 = builder.Entity<T3>(e3.Name, e3.Schema);
                body(builder, entity1, entity2, entity3);
    
                return builder;
            }

        public SqlBuilder Query<T1, T2, T3, T4>(
            Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>> body,
            SqlEntityConfig e1 = default,
            SqlEntityConfig e2 = default,
            SqlEntityConfig e3 = default,
            SqlEntityConfig e4 = default)
        {
            var entity1 = builder.Entity<T1>(e1.Name, e1.Schema);
            var entity2 = builder.Entity<T2>(e2.Name, e2.Schema);
            var entity3 = builder.Entity<T3>(e3.Name, e3.Schema);
            var entity4 = builder.Entity<T4>(e4.Name, e4.Schema);
            body(entity1, entity2, entity3, entity4);

            return builder;
        }

        public SqlBuilder Query<T1, T2, T3, T4>(
            Action<SqlBuilder, ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>> body,
            SqlEntityConfig e1 = default,
            SqlEntityConfig e2 = default,
            SqlEntityConfig e3 = default,
            SqlEntityConfig e4 = default)
        {
            var entity1 = builder.Entity<T1>(e1.Name, e1.Schema);
            var entity2 = builder.Entity<T2>(e2.Name, e2.Schema);
            var entity3 = builder.Entity<T3>(e3.Name, e3.Schema);
            var entity4 = builder.Entity<T4>(e4.Name, e4.Schema);
            body(builder, entity1, entity2, entity3, entity4);

            return builder;
        }

        public SqlBuilder Query<T1, T2, T3, T4, T5>(
            Action<ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>, ISqlEntity<T5>> body,
            SqlEntityConfig e1 = default,
            SqlEntityConfig e2 = default,
            SqlEntityConfig e3 = default,
            SqlEntityConfig e4 = default,
            SqlEntityConfig e5 = default)
        {
            var entity1 = builder.Entity<T1>(e1.Name, e1.Schema);
            var entity2 = builder.Entity<T2>(e2.Name, e2.Schema);
            var entity3 = builder.Entity<T3>(e3.Name, e3.Schema);
            var entity4 = builder.Entity<T4>(e4.Name, e4.Schema);
            var entity5 = builder.Entity<T5>(e5.Name, e5.Schema);
            body(entity1, entity2, entity3, entity4, entity5);

            return builder;
        }

        public SqlBuilder Query<T1, T2, T3, T4, T5>(
            Action<SqlBuilder, ISqlEntity<T1>, ISqlEntity<T2>, ISqlEntity<T3>, ISqlEntity<T4>, ISqlEntity<T5>> body,
            SqlEntityConfig e1 = default,
            SqlEntityConfig e2 = default,
            SqlEntityConfig e3 = default,
            SqlEntityConfig e4 = default,
            SqlEntityConfig e5 = default)
        {
            var entity1 = builder.Entity<T1>(e1.Name, e1.Schema);
            var entity2 = builder.Entity<T2>(e2.Name, e2.Schema);
            var entity3 = builder.Entity<T3>(e3.Name, e3.Schema);
            var entity4 = builder.Entity<T4>(e4.Name, e4.Schema);
            var entity5 = builder.Entity<T5>(e5.Name, e5.Schema);
            body(builder, entity1, entity2, entity3, entity4, entity5);

            return builder;
        }
    }
}