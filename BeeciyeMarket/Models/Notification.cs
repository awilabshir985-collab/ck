using System.ComponentModel.DataAnnotations;

namespace BeeciyeMarket.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Link { get; set; }

        public NotificationType Type { get; set; } = NotificationType.System;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
