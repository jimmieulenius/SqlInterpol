using SqlInterpol.Schema;

namespace SqlInterpol.Test.Models;

[SqlTable("Users")]
public class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}