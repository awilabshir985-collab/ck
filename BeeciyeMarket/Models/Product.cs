using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeeciyeMarket.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 100000, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; }

        public int InitialQuantity { get; set; }

        [Required]
        public ProductCondition Condition { get; set; } = ProductCondition.Good;

        public string? ImagePath { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? SellerId { get; set; }
        public ApplicationUser? Seller { get; set; }

        [StringLength(150)]
        public string? GuestName { get; set; }

        [StringLength(30)]
        public string? GuestPhone { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        [NotMapped]
        public decimal UnitPrice => Price / Math.Max(1, InitialQuantity);

        [NotMapped]
        public decimal CurrentValue => UnitPrice * Quantity;

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        [NotMapped]
        public List<IFormFile>? ImageFiles { get; set; }
    }
}
