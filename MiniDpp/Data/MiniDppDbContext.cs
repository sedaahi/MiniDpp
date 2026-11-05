using Microsoft.EntityFrameworkCore;
using MiniDpp.Models;

namespace MiniDpp.Data;

public class MiniDppDbContext(DbContextOptions<MiniDppDbContext> options)
    : DbContext(options)
{
    public DbSet<ProductModel> ProductModels => Set<ProductModel>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    public DbSet<ProductBatchVersion> ProductBatchVersions =>
        Set<ProductBatchVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductBatch>()
            .HasOne(batch => batch.ProductModel)
            .WithMany(model => model.Batches)
            .HasForeignKey(batch => batch.ProductModelId);

        modelBuilder.Entity<ProductBatch>()
            .HasIndex(batch => batch.Gtin)
            .IsUnique();

        modelBuilder.Entity<ProductBatch>()
            .Property(batch => batch.Version)
            .HasDefaultValue(1)
            .IsConcurrencyToken();

        modelBuilder.Entity<ProductBatchVersion>()
            .HasOne(version => version.ProductBatch)
            .WithMany()
            .HasForeignKey(version => version.ProductBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductBatchVersion>()
            .HasIndex(version => new
            {
                version.ProductBatchId,
                version.Version
            })
            .IsUnique();
    }
}