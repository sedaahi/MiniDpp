using Microsoft.EntityFrameworkCore;
using MiniDpp.Models;

namespace MiniDpp.Data;

public class MiniDppDbContext(DbContextOptions<MiniDppDbContext> options)
    : DbContext(options)
{
    public DbSet<ProductModel> ProductModels => Set<ProductModel>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductBatch>()
            .HasOne(batch => batch.ProductModel)
            .WithMany(model => model.Batches)
            .HasForeignKey(batch => batch.ProductModelId);

        modelBuilder.Entity<ProductBatch>()
            .HasIndex(batch => batch.Gtin)
            .IsUnique();
    }
}