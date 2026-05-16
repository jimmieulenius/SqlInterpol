using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

[SqlTable("MyTable")]
public record TableOnlyModel(int Id);