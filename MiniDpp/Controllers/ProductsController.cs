using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Data;

namespace MiniDpp.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(MiniDppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var products = await db.ProductBatches
            .AsNoTracking()
            .OrderBy(batch => batch.ProductModel.Name)
            .ThenBy(batch => batch.Gtin)
            .Select(batch => new
            {
                batch.Gtin,
                ModelName = batch.ProductModel.Name,
                batch.BatchNumber,
                batch.ProductionLocation
            })
            .ToListAsync(cancellationToken);

        return Ok(products);
    }
}