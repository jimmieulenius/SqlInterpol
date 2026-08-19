using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class UpdateTestSuite
{
    [SqlTable("Orders", "dbo")]
    public class OrderWithIgnoreModel
    {
        public int Id { get; set; }
        [SqlColumn("order_status")]
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
        [SqlIgnore]
        public string InternalNotes { get; set; } = "";
    }
}