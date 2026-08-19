using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class SegmentRewriterTestSuite
{
    [SqlTable("Orders", "dbo")]
    public class Order
    {
        public int Id { get; set; }
    }
}