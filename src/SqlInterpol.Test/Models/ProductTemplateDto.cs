
namespace SqlInterpol.Test.Models;

public record ProductTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
}