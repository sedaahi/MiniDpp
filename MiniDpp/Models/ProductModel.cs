namespace MiniDpp.Models;

public class ProductModel
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<ProductBatch> Batches { get; } = new List<ProductBatch>();
}