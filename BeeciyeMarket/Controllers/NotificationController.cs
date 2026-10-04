using BeeciyeMarket.Data;
using BeeciyeMarket.Models;
using BeeciyeMarket.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeeciyeMarket.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notifications;

        public NotificationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool unreadOnly = false)
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Notifications.Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(200)
                .ToListAsync();

            ViewBag.UnreadOnly = unreadOnly;
            ViewBag.ActiveNav = "Notifications";
            return View(notifications);
        }

        // Marks the notification as read, then follows its link.
        [HttpGet]
        public async Task<IActionResult> Open(int id)
        {
            var userId = _userManager.GetUserId(User);
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification == null)
            {
                return NotFound();
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(notification.Link) && Url.IsLocalUrl(notification.Link))
            {
                return LocalRedirect(notification.Link);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            await _context.Notifications
                .Where(n => n.Id == id && n.UserId == userId)
                .ExecuteDeleteAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearRead()
        {
            var userId = _userManager.GetUserId(User);
            await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead)
                .ExecuteDeleteAsync();

            return RedirectToAction(nameof(Index));
        }

        // Polled by the navbar bell to refresh the unread badge.
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = _userManager.GetUserId(User)!;
            return Json(new { count = await _notifications.GetUnreadCountAsync(userId) });
        }
    }
}
