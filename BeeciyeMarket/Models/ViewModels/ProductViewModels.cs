using System.ComponentModel.DataAnnotations;

namespace BeeciyeMarket.Models.ViewModels
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int? SelectedCategoryId { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 8;
    }

    public class CartViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal Subtotal => Items.Sum(i => i.Product!.UnitPrice * i.Quantity);
        public decimal Shipping { get; set; } = 0;
        public decimal Total => Subtotal + Shipping;
    }

    public class CheckoutViewModel : IValidatableObject
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal Subtotal => Items.Sum(i => i.Product!.UnitPrice * i.Quantity);
        public decimal DeliveryFee => DeliveryMethod.ToFee();
        public decimal Total => Subtotal + DeliveryFee;

        // Set when buying a single product directly (Buy Now) instead of the cart
        public int? BuyNowProductId { get; set; }
        public int BuyNowQuantity { get; set; } = 1;

        // 1. Contact
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        [Display(Name = "Full Name")]
        public string ShippingFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string ShippingPhone { get; set; } = string.Empty;

        // 2. Delivery
        [Display(Name = "Delivery Method")]
        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.HomeDelivery;

        // 3. Address (not needed for pickup)
        [StringLength(100)]
        [Display(Name = "City")]
        public string? ShippingCity { get; set; }

        [StringLength(100)]
        [Display(Name = "District / Neighbourhood")]
        public string? ShippingDistrict { get; set; }

        [StringLength(300)]
        [Display(Name = "Street / Landmark")]
        public string? ShippingAddress { get; set; }

        [StringLength(300)]
        [Display(Name = "Delivery Notes (optional)")]
        public string? DeliveryNotes { get; set; }

        // 4. Payment
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        [Display(Name = "Provider")]
        public MobileMoneyProvider? Provider { get; set; }

        [Phone]
        [StringLength(20)]
        [Display(Name = "Number you paid from")]
        public string? PayerPhone { get; set; }

        [StringLength(100)]
        [Display(Name = "Transaction ID")]
        public string? TransactionReference { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DeliveryMethod != DeliveryMethod.Pickup)
            {
                if (string.IsNullOrWhiteSpace(ShippingCity))
                    yield return new ValidationResult("City is required for delivery.", new[] { nameof(ShippingCity) });
                if (string.IsNullOrWhiteSpace(ShippingDistrict))
                    yield return new ValidationResult("District is required for delivery.", new[] { nameof(ShippingDistrict) });
                if (string.IsNullOrWhiteSpace(ShippingAddress))
                    yield return new ValidationResult("Street or landmark is required for delivery.", new[] { nameof(ShippingAddress) });
            }

            if (PaymentMethod == PaymentMethod.MobileMoney)
            {
                if (Provider == null)
                    yield return new ValidationResult("Choose the mobile money provider you paid with.", new[] { nameof(Provider) });
                if (string.IsNullOrWhiteSpace(PayerPhone))
                    yield return new ValidationResult("Enter the number you paid from.", new[] { nameof(PayerPhone) });
                if (string.IsNullOrWhiteSpace(TransactionReference))
                    yield return new ValidationResult("Enter the transaction ID from your payment SMS.", new[] { nameof(TransactionReference) });
            }
        }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public List<Product> RecentProducts { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
    }

    public class SellerDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public List<Product> RecentProducts { get; set; } = new();
    }

    public class ReportViewModel
    {
        public DateTime StartDate { get; set; } = DateTime.Now.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.Now;
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalProfit { get; set; }
        public List<DailySales> MonthlySales { get; set; } = new();
    }

    public class DailySales
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
