using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class UpdateTestSuite
{
    [SqlTable("Products", "dbo")]
    public class Product
    {
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
    }
}