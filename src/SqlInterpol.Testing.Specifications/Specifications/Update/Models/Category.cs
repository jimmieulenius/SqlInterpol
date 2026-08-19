using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class UpdateTestSuite
{
    [SqlTable("Category")]
    public class Category
    {
        public int Id { get; set; }
    }
}