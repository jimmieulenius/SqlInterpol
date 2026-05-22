namespace SqlInterpol.Test.Models;

public enum ProductStatus { OutOfStock = 0, Available = 1 }
public enum ProductCategoryType { Electronics = 1, Furniture = 2 }

[SqlTable("tbl_complex_products")]
public class ComplexProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Should default to Integer (per our SqlInterpolOptions)
    public ProductStatus Status { get; set; }
    
    // Should be forced to String
    [SqlEnumFormat(SqlEnumFormat.String)]
    public ProductCategoryType Category { get; set; }
    
    // COMPLEX TYPE: Should be completely ignored!
    public Supplier Supplier { get; set; } = new();
}