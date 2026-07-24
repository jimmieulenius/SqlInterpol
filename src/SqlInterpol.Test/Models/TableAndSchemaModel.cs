
using SqlInterpol.Schema;

namespace SqlInterpol.Test.Models;

[SqlTable(name: "MyTable", schema: "MySchema")]
public record TableAndSchemaModel(int Id);