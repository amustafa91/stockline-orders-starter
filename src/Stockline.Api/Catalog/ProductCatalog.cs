public sealed class ProductCatalog
{
    // Local teaching catalog. The server owns these prices.
    private readonly Product[] _products =
    [
        new("BRG-204", 12m, "USD"),
        new("CAB-105", 7.50m, "USD")
    ];

    public Product? Find(string sku) =>
        _products.FirstOrDefault(product => product.Sku == sku);
}
