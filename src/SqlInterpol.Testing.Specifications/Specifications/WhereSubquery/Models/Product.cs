using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class WhereSubqueryTestSuite
{
    [SqlTable("Products", "dbo")]
    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
    }
}