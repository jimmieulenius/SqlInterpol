using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class FromTestSuite
{
    [SqlTable(name: "MyTable", schema: "MySchema")]
    public record TableAndSchemaModel(int Id);
}