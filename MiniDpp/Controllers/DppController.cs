using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Data;
using MiniDpp.Models;
using QRCoder;

namespace MiniDpp.Controllers;

[Route("dpp")]
public sealed class DppController(MiniDppDbContext db) : Controller
{
    [HttpGet("{gtin}", Name = "ProductPassport")]
    public async Task<IActionResult> Passport(
        string gtin,
        [FromQuery] string? rol,
        CancellationToken cancellationToken)
    {
        var passport = await FindPassportAsync(gtin, rol, cancellationToken);

        if (passport is null)
            return NotFound("Bu GTIN için ürün pasaportu bulunamadı.");

        return View(passport);
    }

    [HttpGet("{gtin}.jsonld")]
    public async Task<IActionResult> JsonLd(
        string gtin,
        [FromQuery] string? rol,
        CancellationToken cancellationToken)
    {
        var passport = await FindPassportAsync(gtin, rol, cancellationToken);

        if (passport is null)
            return NotFound("Bu GTIN için ürün pasaportu bulunamadı.");

        var properties = new List<object>
        {
            Property("Parti numarası", passport.BatchNumber),
            Property("Üretim yeri", passport.ProductionLocation),
            Property("Pamuk oranı (%)", passport.CottonPercentage),
            Property("Elastan oranı (%)", passport.ElastanePercentage)
        };

        if (passport.CarbonKgCo2e is decimal carbon)
            properties.Add(Property("Karbon ayak izi (kg CO₂e)", carbon));

        var document = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = passport.ModelName,
            ["gtin14"] = passport.Gtin,
            ["additionalProperty"] = properties
        };

        return Content(JsonSerializer.Serialize(document), "application/ld+json");
    }

    [HttpGet("{gtin}/history")]
    public async Task<IActionResult> History(
        string gtin,
        [FromQuery] string? rol,
        CancellationToken cancellationToken)
    {
        var batch = await db.ProductBatches
            .AsNoTracking()
            .Include(batch => batch.ProductModel)
            .SingleOrDefaultAsync(
                batch => batch.Gtin == gtin, cancellationToken);

        if (batch is null)
            return NotFound("Bu GTIN için ürün pasaportu bulunamadı.");

        var previousVersions = await db.ProductBatchVersions
            .AsNoTracking()
            .Where(version => version.ProductBatchId == batch.Id)
            .OrderByDescending(version => version.Version)
            .ToListAsync(cancellationToken);

        var showCarbon = rol == "yetkili";

        var versions = new List<Dictionary<string, object?>>
        {
            VersionDocument(
                batch.Version,
                batch.UpdatedAtUtc,
                true,
                batch.ProductModel.Name,
                batch.BatchNumber,
                batch.ProductionLocation,
                batch.CottonPercentage,
                batch.ElastanePercentage,
                showCarbon ? batch.CarbonKgCo2e : null)
        };

        versions.AddRange(previousVersions.Select(version =>
            VersionDocument(
                version.Version,
                version.UpdatedAtUtc,
                false,
                version.ModelName,
                version.BatchNumber,
                version.ProductionLocation,
                version.CottonPercentage,
                version.ElastanePercentage,
                showCarbon ? version.CarbonKgCo2e : null)));

        return Ok(new { gtin, versions });
    }

    [HttpGet("{gtin}/qr.png")]
    public async Task<IActionResult> QrCode(
        string gtin,
        CancellationToken cancellationToken)
    {
        var exists = await db.ProductBatches
            .AnyAsync(batch => batch.Gtin == gtin, cancellationToken);

        if (!exists)
            return NotFound("Bu GTIN için ürün pasaportu bulunamadı.");

        var passportUrl = Url.RouteUrl(
            "ProductPassport",
            new { gtin },
            Request.Scheme)
            ?? throw new InvalidOperationException(
                "Pasaport adresi oluşturulamadı.");

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(
            passportUrl, QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(data);

        return File(qr.GetGraphic(8), "image/png");
    }

    private async Task<ProductPassportViewModel?> FindPassportAsync(
        string gtin,
        string? rol,
        CancellationToken cancellationToken)
    {
        var batch = await db.ProductBatches
            .AsNoTracking()
            .Include(batch => batch.ProductModel)
            .SingleOrDefaultAsync(
                batch => batch.Gtin == gtin, cancellationToken);

        if (batch is null)
            return null;

        return new ProductPassportViewModel(
            batch.Gtin,
            batch.ProductModel.Name,
            batch.BatchNumber,
            batch.ProductionLocation,
            batch.CottonPercentage,
            batch.ElastanePercentage,
            rol == "yetkili" ? batch.CarbonKgCo2e : null);
    }

    private static Dictionary<string, object?> VersionDocument(
        int version,
        DateTime? updatedAtUtc,
        bool isCurrent,
        string modelName,
        string batchNumber,
        string productionLocation,
        decimal cottonPercentage,
        decimal elastanePercentage,
        decimal? carbonKgCo2e)
    {
        var document = new Dictionary<string, object?>
        {
            ["version"] = version,
            ["updatedAtUtc"] = updatedAtUtc is DateTime date
                ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                : null,
            ["isCurrent"] = isCurrent,
            ["modelName"] = modelName,
            ["batchNumber"] = batchNumber,
            ["productionLocation"] = productionLocation,
            ["cottonPercentage"] = cottonPercentage,
            ["elastanePercentage"] = elastanePercentage
        };

        if (carbonKgCo2e is decimal carbon)
            document["carbonKgCo2e"] = carbon;

        return document;
    }

    private static Dictionary<string, object> Property(
        string name, object value) =>
        new()
        {
            ["@type"] = "PropertyValue",
            ["name"] = name,
            ["value"] = value
        };
}