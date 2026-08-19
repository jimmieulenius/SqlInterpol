using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class JoinSubqueryTestSuite
{
    [SqlTable(name: "Products", schema: "dbo")]
    public class Product
    {
        public int Id { get; set; }
        [SqlColumn("PROD_NAME")]
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
    }
}