namespace MiniDpp.Models;

public sealed record ProductPassportViewModel(
    string Gtin,
    string ModelName,
    string BatchNumber,
    string ProductionLocation,
    decimal CottonPercentage,
    decimal ElastanePercentage,
    decimal? CarbonKgCo2e);