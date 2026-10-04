using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeeciyeMarket.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string? CustomerId { get; set; }
        public ApplicationUser? Customer { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        [Display(Name = "Full Name")]
        public string ShippingFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string ShippingPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(300)]
        [Display(Name = "Delivery Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
