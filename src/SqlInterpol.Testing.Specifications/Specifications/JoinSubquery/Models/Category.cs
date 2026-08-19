namespace SqlInterpol.Testing.Specifications;

public abstract partial class JoinSubqueryTestSuite
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}