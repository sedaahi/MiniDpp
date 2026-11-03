using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Data;
using MiniDpp.Models;
using MiniDpp.Services;

namespace MiniDpp.Controllers;

[ApiController]
[Route("api/imports")]
public sealed class ProductImportsController(MiniDppDbContext db) : ControllerBase
{
    [HttpPost("csv")]
    public async Task<IActionResult> ImportCsv(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Bir CSV dosyası yükleyin." });

        using var stream = file.OpenReadStream();
        var result = ProductCsvReader.Read(stream);

        if (result.Errors.Any(error => error.LineNumber == 1))
            return BadRequest(new { errors = result.Errors });

        var models = await db.ProductModels.ToDictionaryAsync(
            model => model.Name, cancellationToken);

        var existingGtins = (await db.ProductBatches
            .Select(batch => batch.Gtin)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var imported = 0;
        var skipped = 0;

        foreach (var row in result.ValidRows)
        {
            if (!existingGtins.Add(row.Gtin))
            {
                skipped++;
                continue;
            }

            if (!models.TryGetValue(row.ModelName, out var model))
            {
                model = new ProductModel { Name = row.ModelName };
                models.Add(row.ModelName, model);
                db.ProductModels.Add(model);
            }

            ProductCsvValidator.TryParseDecimal(row.CottonPercentage, out var cotton);
            ProductCsvValidator.TryParseDecimal(row.ElastanePercentage, out var elastane);
            ProductCsvValidator.TryParseDecimal(row.CarbonKgCo2e, out var carbon);

            db.ProductBatches.Add(new ProductBatch
            {
                Gtin = row.Gtin,
                BatchNumber = row.BatchNumber,
                ProductionLocation = row.ProductionLocation,
                CottonPercentage = cotton,
                ElastanePercentage = elastane,
                CarbonKgCo2e = carbon,
                ProductModel = model
            });

            imported++;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { imported, skipped, errors = result.Errors });
    }
}