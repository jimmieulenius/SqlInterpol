using SqlInterpol.Schema;

namespace SqlInterpol.Test.Models;

[SqlTable("MyTable")]
public record TableOnlyModel(int Id);