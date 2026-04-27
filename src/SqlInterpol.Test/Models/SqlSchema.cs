using SqlInterpol.Models;

namespace SqlSchema;

public class ProductsTable(string? schema = null) : SqlTable("ReleasedProducts", schema)
{
    private SqlColumn? _itemNumber;
    private SqlColumn? _productName;
    private SqlColumn? _price;

    public SqlColumn ItemNumber => _itemNumber ??= new(this, "ItemNumber");

    public SqlColumn ProductName => _productName ??= new(this, "ProductName");

    public SqlColumn Price => _price ??= new(this, "Price");
}

public static class Dbo
{
    public const string Schema = "dbo";
    public static ProductsTable Products => new(Schema);
}