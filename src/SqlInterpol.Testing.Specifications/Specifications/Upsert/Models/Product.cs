using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class UpsertTestSuite
{
    [SqlTable("Products", "dbo")]
    public class Product
    {
        public int Id { get; set; }
        [SqlColumn("PROD_NAME")]
        public string Name { get; set; } = "";
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
    }
}