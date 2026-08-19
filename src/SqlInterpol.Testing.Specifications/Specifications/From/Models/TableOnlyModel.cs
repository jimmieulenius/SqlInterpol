using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class FromTestSuite
{
    [SqlTable("MyTable")]
    public record TableOnlyModel(int Id);
}