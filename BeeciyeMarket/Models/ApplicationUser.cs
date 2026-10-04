using Microsoft.AspNetCore.Identity;

namespace BeeciyeMarket.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ProfileImagePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
