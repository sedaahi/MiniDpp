using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace MiniDpp.Services;

internal sealed record CsvRowError(int LineNumber, IReadOnlyList<string> Errors);

internal sealed record ProductCsvReadResult(
    IReadOnlyList<ProductCsvRow> ValidRows,
    IReadOnlyList<CsvRowError> Errors);

internal static class ProductCsvReader
{
    private static readonly string[] RequiredHeaders =
    [
        "gtin",
        "model_adi",
        "batch_no",
        "elyaf_pamuk_yuzde",
        "elyaf_elastan_yuzde",
        "uretim_yeri",
        "karbon_kg_co2e"
    ];

    public static ProductCsvReadResult Read(Stream stream)
    {
        using var parser = new TextFieldParser(stream, Encoding.UTF8, true, true)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(",");

        var validRows = new List<ProductCsvRow>();
        var errors = new List<CsvRowError>();
        var headers = ReadHeaders(parser, errors);

        if (headers is null)
            return new ProductCsvReadResult(validRows, errors);

        while (!parser.EndOfData)
        {
            var lineNumber = (int)parser.LineNumber;
            string[]? fields;

            try
            {
                fields = parser.ReadFields();
            }
            catch (MalformedLineException exception)
            {
                errors.Add(new CsvRowError(
                    (int)exception.LineNumber, ["CSV satırının biçimi geçersiz."]));
                continue;
            }

            if (fields is null)
                break;

            if (fields.Length != headers.Count)
            {
                errors.Add(new CsvRowError(
                    lineNumber, ["Sütun sayısı başlıkla eşleşmiyor."]));
                continue;
            }

            var row = new ProductCsvRow(
                lineNumber,
                fields[headers["gtin"]],
                fields[headers["model_adi"]],
                fields[headers["batch_no"]],
                fields[headers["elyaf_pamuk_yuzde"]],
                fields[headers["elyaf_elastan_yuzde"]],
                fields[headers["uretim_yeri"]],
                fields[headers["karbon_kg_co2e"]]);

            var rowErrors = ProductCsvValidator.Validate(row);
            if (rowErrors.Count == 0)
                validRows.Add(row);
            else
                errors.Add(new CsvRowError(lineNumber, rowErrors));
        }

        return new ProductCsvReadResult(validRows, errors);
    }

    private static Dictionary<string, int>? ReadHeaders(
        TextFieldParser parser, List<CsvRowError> errors)
    {
        string[]? fields;

        try
        {
            fields = parser.ReadFields();
        }
        catch (MalformedLineException)
        {
            errors.Add(new CsvRowError(1, ["CSV başlığının biçimi geçersiz."]));
            return null;
        }

        if (fields is null)
        {
            errors.Add(new CsvRowError(1, ["CSV dosyası boş."]));
            return null;
        }

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < fields.Length; i++)
        {
            if (!headers.TryAdd(fields[i], i))
            {
                errors.Add(new CsvRowError(1, [$"Tekrarlanan sütun: {fields[i]}"]));
                return null;
            }
        }

        var missingHeaders = RequiredHeaders
            .Where(header => !headers.ContainsKey(header))
            .ToArray();

        if (missingHeaders.Length == 0)
            return headers;

        errors.Add(new CsvRowError(
            1, [$"Eksik sütunlar: {string.Join(", ", missingHeaders)}"]));
        return null;
    }
}