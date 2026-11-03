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
        var batch = await db.ProductBatches
            .AsNoTracking()
            .Include(batch => batch.ProductModel)
            .SingleOrDefaultAsync(batch => batch.Gtin == gtin, cancellationToken);

        if (batch is null)
            return NotFound("Bu GTIN için ürün pasaportu bulunamadı.");

        var passport = new ProductPassportViewModel(
            batch.Gtin,
            batch.ProductModel.Name,
            batch.BatchNumber,
            batch.ProductionLocation,
            batch.CottonPercentage,
            batch.ElastanePercentage,
            rol == "yetkili" ? batch.CarbonKgCo2e : null);

        return View(passport);
    }
}