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

    public class CheckoutViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal Subtotal => Items.Sum(i => i.Product!.UnitPrice * i.Quantity);
        public decimal Shipping { get; set; } = 0;
        public decimal Total => Subtotal + Shipping;

        [Display(Name = "Full Name")]
        public string ShippingFullName { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string ShippingPhone { get; set; } = string.Empty;

        [Display(Name = "Delivery Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;
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
