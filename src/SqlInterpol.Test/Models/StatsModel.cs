using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

[SqlTable("Stats")]
public record StatsModel(int CategoryId, decimal MaxPrice);