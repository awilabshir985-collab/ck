using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeeciyeMarket.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public string? TransactionReference { get; set; }

        public MobileMoneyProvider? Provider { get; set; }

        [StringLength(20)]
        public string? PayerPhone { get; set; }
    }
}
