using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class UpdateTestSuite
{
    [SqlTable("Users")]
    public class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}