using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class SelectSubqueryTestSuite
{
    [SqlGeneratorIgnore]
    [SqlTable("Category")]
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int IsActive { get; set; }
    }
}