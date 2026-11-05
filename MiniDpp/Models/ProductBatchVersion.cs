namespace MiniDpp.Models;

public class ProductBatchVersion
{
    public int Id { get; set; }

    public int ProductBatchId { get; set; }
    public ProductBatch ProductBatch { get; set; } = null!;

    public required string Gtin { get; set; }
    public required string ModelName { get; set; }
    public required string BatchNumber { get; set; }
    public decimal CottonPercentage { get; set; }
    public decimal ElastanePercentage { get; set; }
    public required string ProductionLocation { get; set; }
    public decimal CarbonKgCo2e { get; set; }

    public int Version { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime ArchivedAtUtc { get; set; }
}