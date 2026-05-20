namespace SqlInterpol.Test.Models;

public class CategoryNode
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? ParentId { get; set; }
    public int Depth { get; set; }
}
