using System.Globalization;

namespace MiniDpp.Services;

internal static class ProductCsvValidator
{
    public static List<string> Validate(ProductCsvRow row)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(row.Gtin))
            errors.Add("GTIN boş olamaz.");
        else if (row.Gtin.Length != 14 || row.Gtin.Any(c => c is < '0' or > '9'))
            errors.Add("GTIN 14 rakamdan oluşmalıdır.");
        else if (!HasValidCheckDigit(row.Gtin))
            errors.Add("GTIN kontrol basamağı geçersiz.");

        if (string.IsNullOrWhiteSpace(row.ModelName))
            errors.Add("Model adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(row.BatchNumber))
            errors.Add("Parti numarası boş olamaz.");

        if (string.IsNullOrWhiteSpace(row.ProductionLocation))
            errors.Add("Üretim yeri boş olamaz.");

        var cottonValid = TryParseDecimal(row.CottonPercentage, out var cotton)
            && cotton is >= 0 and <= 100;
        var elastaneValid = TryParseDecimal(row.ElastanePercentage, out var elastane)
            && elastane is >= 0 and <= 100;

        if (!cottonValid)
            errors.Add("Pamuk oranı 0 ile 100 arasında bir sayı olmalıdır.");

        if (!elastaneValid)
            errors.Add("Elastan oranı 0 ile 100 arasında bir sayı olmalıdır.");

        if (cottonValid && elastaneValid && cotton + elastane != 100m)
            errors.Add("Pamuk ve elastan oranlarının toplamı %100 olmalıdır.");

        if (!TryParseDecimal(row.CarbonKgCo2e, out var carbon) || carbon < 0)
            errors.Add("Karbon değeri sıfır veya pozitif bir sayı olmalıdır.");

        return errors;
    }

    internal static bool TryParseDecimal(string value, out decimal number) =>
        decimal.TryParse(
            value.Trim(),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out number);

    private static bool HasValidCheckDigit(string gtin)
    {
        var sum = 0;

        for (var i = 0; i < 13; i++)
        {
            var digit = gtin[i] - '0';
            var weight = i % 2 == 0 ? 3 : 1;
            sum += digit * weight;
        }

        var expectedCheckDigit = (10 - sum % 10) % 10;
        return expectedCheckDigit == gtin[13] - '0';
    }
}