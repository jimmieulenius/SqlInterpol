using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class SelectTestSuite
{
    [SqlTable("Products", "dbo")]
    public class ProductWithIgnoreModel
    {
        public int Id { get; set; }
        [SqlColumn("PROD_NAME")]
        public string Name { get; set; } = "";
        [SqlIgnore]
        public string IgnoredProperty { get; set; } = "";
    }
}