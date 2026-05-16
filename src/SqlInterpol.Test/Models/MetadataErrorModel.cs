using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

public class MetadataErrorModel
{
    public int Id { get; set; }
    
    [SqlIgnore]
    public string UnmappedProperty { get; set; } = "";
}