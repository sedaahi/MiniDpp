using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Data;
using MiniDpp.Models;

namespace MiniDpp.Controllers;

[Route("dpp")]
public sealed class DppController(MiniDppDbContext db) : Controller
{
    [HttpGet("{gtin}")]
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

    private async Task<ProductPassportViewModel?> FindPassportAsync(
        string gtin,
        string? rol,
        CancellationToken cancellationToken)
    {
        var batch = await db.ProductBatches
            .AsNoTracking()
            .Include(batch => batch.ProductModel)
            .SingleOrDefaultAsync(batch => batch.Gtin == gtin, cancellationToken);

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

    private static Dictionary<string, object> Property(string name, object value) =>
        new()
        {
            ["@type"] = "PropertyValue",
            ["name"] = name,
            ["value"] = value
        };
}