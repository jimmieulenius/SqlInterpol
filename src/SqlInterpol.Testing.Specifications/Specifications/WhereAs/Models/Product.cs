using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class WhereAsTestSuite
{
    [SqlTable("Products", "dbo")]
    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
    }
}