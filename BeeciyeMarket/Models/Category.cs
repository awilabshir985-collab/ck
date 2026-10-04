using System.ComponentModel.DataAnnotations;

namespace BeeciyeMarket.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        public string? IconClass { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
