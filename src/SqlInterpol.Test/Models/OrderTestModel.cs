
namespace SqlInterpol.Test.Models;

public class OrderTestModel
{
    public int Id { get; set; }
    
    [SqlIgnore] 
    public string UnmappedProperty { get; set; } = "";
}