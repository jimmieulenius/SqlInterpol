namespace SqlInterpol;

public static partial class SqlBuilderExtensions
{
    extension (SqlBuilder builder)
    {
        // Start the chain
        public SqlEntityFluentBuilder<T1> Entity<T1>(string? name = null, string? schema = null) 
            => new(builder, builder.CreateEntity<T1>(name, schema));
    }
}