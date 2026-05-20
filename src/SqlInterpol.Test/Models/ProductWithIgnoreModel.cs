
namespace SqlInterpol.Test.Models;

[SqlTable("Products", Schema = "dbo")]
public record ProductWithIgnoreModel
{
    public int Id { get; init; }

    [SqlColumn("PROD_NAME")]
    public string Name { get; init; } = "";

    [SqlIgnore]
    public string RuntimeCacheToken { get; init; } = "";
}