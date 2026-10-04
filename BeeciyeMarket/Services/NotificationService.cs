using BeeciyeMarket.Data;
using BeeciyeMarket.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BeeciyeMarket.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(string? userId, string title, string message, string? link = null, NotificationType type = NotificationType.System);
        Task NotifyManyAsync(IEnumerable<string?> userIds, string title, string message, string? link = null, NotificationType type = NotificationType.System);
        Task NotifyAdminsAsync(string title, string message, string? link = null, NotificationType type = NotificationType.System);
        Task NotifyOrderPlacedAsync(int orderId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<List<Notification>> GetRecentAsync(string userId, int count);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Task NotifyAsync(string? userId, string title, string message, string? link = null, NotificationType type = NotificationType.System)
        {
            return NotifyManyAsync(new[] { userId }, title, message, link, type);
        }

        public async Task NotifyManyAsync(IEnumerable<string?> userIds, string title, string message, string? link = null, NotificationType type = NotificationType.System)
        {
            var recipients = userIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

            if (!recipients.Any())
            {
                return;
            }

            foreach (var userId in recipients)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = Truncate(title, 150),
                    Message = Truncate(message, 500),
                    Link = link,
                    Type = type,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task NotifyAdminsAsync(string title, string message, string? link = null, NotificationType type = NotificationType.System)
        {
            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            await NotifyManyAsync(admins.Select(a => (string?)a.Id), title, message, link, type);
        }

        public async Task NotifyOrderPlacedAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return;
            }

            var orderNo = $"#{order.Id:D4}";

            await NotifyAsync(order.CustomerId,
                "Order placed",
                $"Your order {orderNo} of ${order.TotalPrice:0.00} has been received and is pending confirmation.",
                $"/Order/Details/{order.Id}",
                NotificationType.Order);

            foreach (var group in order.OrderItems.Where(oi => oi.SellerId != null).GroupBy(oi => oi.SellerId))
            {
                var names = string.Join(", ", group.Select(oi => $"{oi.Product?.Name} x{oi.Quantity}"));
                await NotifyAsync(group.Key,
                    "New sale",
                    $"{order.ShippingFullName} ordered {names} (order {orderNo}).",
                    "/Order/Sales",
                    NotificationType.Order);

                foreach (var item in group.Where(oi => oi.Product != null && oi.Product.Quantity <= 0))
                {
                    await NotifyAsync(group.Key,
                        "Product sold out",
                        $"'{item.Product!.Name}' is out of stock and has been hidden from the market.",
                        "/Product/MyProducts",
                        NotificationType.Product);
                }
            }

            await NotifyAdminsAsync(
                "New order",
                $"Order {orderNo} for ${order.TotalPrice:0.00} was placed by {order.ShippingFullName}.",
                $"/Admin/OrderDetails/{order.Id}",
                NotificationType.Order);
        }

        public Task<int> GetUnreadCountAsync(string userId)
        {
            return _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public Task<List<Notification>> GetRecentAsync(string userId, int count)
        {
            return _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value.Substring(0, max - 3) + "...";
    }
}
