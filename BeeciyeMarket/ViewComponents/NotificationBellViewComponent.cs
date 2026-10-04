using BeeciyeMarket.Models;
using BeeciyeMarket.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BeeciyeMarket.ViewComponents
{
    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }
        public List<Notification> Recent { get; set; } = new();
    }

    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notifications;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationBellViewComponent(INotificationService notifications, UserManager<ApplicationUser> userManager)
        {
            _notifications = notifications;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            if (string.IsNullOrEmpty(userId))
            {
                return Content(string.Empty);
            }

            var model = new NotificationBellViewModel
            {
                UnreadCount = await _notifications.GetUnreadCountAsync(userId),
                Recent = await _notifications.GetRecentAsync(userId, 6)
            };
            return View(model);
        }
    }
}
