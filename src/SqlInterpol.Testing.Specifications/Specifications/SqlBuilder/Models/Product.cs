using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class SqlBuilderTestSuite
{
    [SqlTable("Products", "dbo")]
    public class Product
    {
        public int Id { get; set; }
    }
}