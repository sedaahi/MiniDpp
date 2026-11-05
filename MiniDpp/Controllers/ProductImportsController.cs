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

        var models = (await db.ProductModels
                .ToListAsync(cancellationToken))
            .GroupBy(model => model.Name, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.Ordinal);

        var batches = await db.ProductBatches
            .Include(batch => batch.ProductModel)
            .ToDictionaryAsync(batch => batch.Gtin, cancellationToken);

        var processedGtins = new HashSet<string>(StringComparer.Ordinal);
        var errors = result.Errors.ToList();

        var imported = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in result.ValidRows)
        {
            // Aynı dosyada aynı GTIN için ilk geçerli satır işlenir.
            if (!processedGtins.Add(row.Gtin))
            {
                errors.Add(new CsvRowError(
                    row.LineNumber,
                    new[]
                    {
                        "Aynı dosyada bu GTIN için birden fazla geçerli satır var."
                    }));

                continue;
            }

            ProductCsvValidator.TryParseDecimal(
                row.CottonPercentage, out var cotton);

            ProductCsvValidator.TryParseDecimal(
                row.ElastanePercentage, out var elastane);

            ProductCsvValidator.TryParseDecimal(
                row.CarbonKgCo2e, out var carbon);

            batches.TryGetValue(row.Gtin, out var batch);

            if (batch is not null &&
                batch.ProductModel.Name == row.ModelName &&
                batch.BatchNumber == row.BatchNumber &&
                batch.ProductionLocation == row.ProductionLocation &&
                batch.CottonPercentage == cotton &&
                batch.ElastanePercentage == elastane &&
                batch.CarbonKgCo2e == carbon)
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

            var now = DateTime.UtcNow;

            if (batch is null)
            {
                batch = new ProductBatch
                {
                    Gtin = row.Gtin,
                    BatchNumber = row.BatchNumber,
                    ProductionLocation = row.ProductionLocation,
                    CottonPercentage = cotton,
                    ElastanePercentage = elastane,
                    CarbonKgCo2e = carbon,
                    ProductModel = model,
                    Version = 1,
                    UpdatedAtUtc = now
                };

                db.ProductBatches.Add(batch);
                batches.Add(row.Gtin, batch);
                imported++;

                continue;
            }

            // Güncel kaydı değiştirmeden önce eski sürümünü sakla.
            db.ProductBatchVersions.Add(new ProductBatchVersion
            {
                ProductBatch = batch,
                Gtin = batch.Gtin,
                ModelName = batch.ProductModel.Name,
                BatchNumber = batch.BatchNumber,
                ProductionLocation = batch.ProductionLocation,
                CottonPercentage = batch.CottonPercentage,
                ElastanePercentage = batch.ElastanePercentage,
                CarbonKgCo2e = batch.CarbonKgCo2e,
                Version = batch.Version,
                UpdatedAtUtc = batch.UpdatedAtUtc,
                ArchivedAtUtc = now
            });

            batch.ProductModel = model;
            batch.BatchNumber = row.BatchNumber;
            batch.ProductionLocation = row.ProductionLocation;
            batch.CottonPercentage = cotton;
            batch.ElastanePercentage = elastane;
            batch.CarbonKgCo2e = carbon;
            batch.Version++;
            batch.UpdatedAtUtc = now;

            updated++;
        }

        try
        {
            // Eski sürümler ve güncellemeler birlikte kaydedilir.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message =
                    "Ürün başka bir işlem tarafından güncellendi. Dosyayı tekrar yükleyin."
            });
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is
                  Microsoft.Data.Sqlite.SqliteException
                  { SqliteErrorCode: 19 })
        {
            return Conflict(new
            {
                message =
                    "Kayıt çakışması nedeniyle dosya kaydedilemedi. Dosyayı tekrar yükleyin."
            });
        }

        return Ok(new
        {
            imported,
            updated,
            skipped,
            errors = errors.OrderBy(error => error.LineNumber)
        });
    }
}