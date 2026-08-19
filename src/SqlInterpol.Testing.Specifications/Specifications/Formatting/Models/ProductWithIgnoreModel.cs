using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class FormattingTestSuite
{
    [SqlTable(name: "Products", schema: "dbo")]
    public record ProductWithIgnoreModel
    {
        public int Id { get; init; }

        [SqlColumn("PROD_NAME")]
        public string Name { get; init; } = "";

        [SqlIgnore]
        public string RuntimeCacheToken { get; init; } = "";
    }
}