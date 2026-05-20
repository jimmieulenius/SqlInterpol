
namespace SqlInterpol.Test.Models;

[SqlTable("Orders", Schema = "dbo")]
    public record OrderWithIgnoreModel
    {
        public int Id { get; init; }

        [SqlColumn("order_status")]
        public string Status { get; init; } = "";
        public decimal Total { get; init; }

        [SqlIgnore]
        public string InternalNotes { get; init; } = "";
    }