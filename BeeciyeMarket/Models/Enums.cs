using System.ComponentModel.DataAnnotations;

namespace BeeciyeMarket.Models
{
    public enum ProductCondition
    {
        New,
        Good,
        Used
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Completed,
        Cancelled
    }

    public enum PaymentMethod
    {
        [Display(Name = "Mobile Payment on Delivery")]
        CashOnDelivery,

        [Display(Name = "Mobile Money")]
        MobileMoney
    }

    public enum PaymentStatus
    {
        Pending,
        Paid,
        Failed
    }

    public enum NotificationType
    {
        System,
        Order,
        Payment,
        Product,
        Account
    }

    public static class NotificationTypeExtensions
    {
        public static string ToIconClass(this NotificationType type) => type switch
        {
            NotificationType.Order => "bi-bag-check",
            NotificationType.Payment => "bi-credit-card",
            NotificationType.Product => "bi-box-seam",
            NotificationType.Account => "bi-person",
            _ => "bi-bell"
        };
    }

    public static class PaymentMethodExtensions
    {
        public static string ToDisplayName(this PaymentMethod method)
        {
            var member = typeof(PaymentMethod).GetMember(method.ToString())[0];
            var display = (DisplayAttribute?)Attribute.GetCustomAttribute(member, typeof(DisplayAttribute));
            return display?.Name ?? method.ToString();
        }
    }
}
