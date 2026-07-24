
using SqlInterpol.Schema;

namespace SqlInterpol.Test.Models;

[SqlTable("Stats")]
public record StatsModel(int CategoryId, decimal MaxPrice);