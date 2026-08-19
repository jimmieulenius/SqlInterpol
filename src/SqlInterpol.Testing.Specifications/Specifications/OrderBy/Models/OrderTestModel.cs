using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class OrderByTestSuite
{
    public class OrderTestModel
{
    public int Id { get; set; }
    
    [SqlIgnore] 
    public string UnmappedProperty { get; set; } = "";
}
}