using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class SelectTestSuite
{
    [SqlTable("tbl_complex_products")]
    public class ComplexProduct
    {
        public string Category { get; set; } = "";
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        
        // Complex type that should be skipped by default scalar projection
        public Supplier Supplier { get; set; } = new();
    }
}