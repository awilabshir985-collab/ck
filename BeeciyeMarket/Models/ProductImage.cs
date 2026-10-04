namespace BeeciyeMarket.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string ImagePath { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }
}
