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

    public enum MobileMoneyProvider
    {
        [Display(Name = "EVC Plus")]
        EvcPlus,

        [Display(Name = "Zaad")]
        Zaad,

        [Display(Name = "Sahal")]
        Sahal,

        [Display(Name = "eDahab")]
        EDahab
    }

    public enum DeliveryMethod
    {
        [Display(Name = "Home Delivery")]
        HomeDelivery,

        [Display(Name = "Express Delivery")]
        Express,

        [Display(Name = "Pickup from Seller")]
        Pickup
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

    public static class EnumDisplayExtensions
    {
        public static string ToDisplayName(this Enum value)
        {
            var member = value.GetType().GetMember(value.ToString())[0];
            var display = (DisplayAttribute?)Attribute.GetCustomAttribute(member, typeof(DisplayAttribute));
            return display?.Name ?? value.ToString();
        }
    }

    public static class DeliveryMethodExtensions
    {
        public static decimal ToFee(this DeliveryMethod method) => method switch
        {
            DeliveryMethod.HomeDelivery => 2.00m,
            DeliveryMethod.Express => 5.00m,
            _ => 0m
        };

        public static string ToDescription(this DeliveryMethod method) => method switch
        {
            DeliveryMethod.HomeDelivery => "Delivered to your address within 1-3 days",
            DeliveryMethod.Express => "Same-day delivery within the city",
            _ => "Collect the item from the seller yourself"
        };

        public static string ToIconClass(this DeliveryMethod method) => method switch
        {
            DeliveryMethod.HomeDelivery => "bi-truck",
            DeliveryMethod.Express => "bi-lightning-charge",
            _ => "bi-shop"
        };
    }
}
