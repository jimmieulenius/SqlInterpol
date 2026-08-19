using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class TemplateTestSuite
{
    [SqlTable("Orders", "dbo")]
    public class TemplateOrderModel
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
    }
}