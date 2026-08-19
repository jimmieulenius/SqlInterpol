using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class UpdateSubqueryTestSuite
{
    [SqlTable("OrderStats")]
    public class OrderStatsModel
    {
        public int CategoryId { get; set; }
        
        [SqlColumn("max_price")]
        public decimal MaxPrice { get; set; }
    }
}