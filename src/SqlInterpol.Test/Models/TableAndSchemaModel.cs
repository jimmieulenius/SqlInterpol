using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

[SqlTable("MyTable", Schema = "MySchema")]
public record TableAndSchemaModel(int Id);