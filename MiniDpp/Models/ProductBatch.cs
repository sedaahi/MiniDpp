namespace MiniDpp.Models;

public class ProductBatch
{
    public int Id { get; set; }
    public required string Gtin { get; set; }
    public required string BatchNumber { get; set; }
    public decimal CottonPercentage { get; set; }
    public decimal ElastanePercentage { get; set; }
    public required string ProductionLocation { get; set; }
    public decimal CarbonKgCo2e { get; set; }

    public int ProductModelId { get; set; }
    public ProductModel ProductModel { get; set; } = null!;
}