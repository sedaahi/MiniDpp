namespace MiniDpp.Services;

internal sealed record ProductCsvRow(
    int LineNumber, //CSV’deki fiziksel satır numarası - Başlık 1, ilk ürün 2.
    string Gtin,
    string ModelName,
    string BatchNumber,
    string CottonPercentage,
    string ElastanePercentage,
    string ProductionLocation,
    string CarbonKgCo2e);