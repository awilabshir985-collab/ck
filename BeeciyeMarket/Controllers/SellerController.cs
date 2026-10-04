using BeeciyeMarket.Data;
using BeeciyeMarket.Models;
using BeeciyeMarket.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeeciyeMarket.Controllers
{
    [Authorize]
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SellerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var sellerId = _userManager.GetUserId(User);

            var orderItems = _context.OrderItems.Include(oi => oi.Order).Where(oi => oi.SellerId == sellerId);

            var model = new SellerDashboardViewModel
            {
                TotalProducts = await _context.Products.CountAsync(p => p.SellerId == sellerId),
                TotalOrders = await orderItems.Select(oi => oi.OrderId).Distinct().CountAsync(),
                TotalSales = await orderItems.SumAsync(oi => (decimal?)oi.TotalPrice) ?? 0,
                RecentProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.SellerId == sellerId)
                    .OrderByDescending(p => p.DateAdded)
                    .Take(5)
                    .ToListAsync()
            };

            ViewBag.ActiveNav = "Dashboard";
            return View(model);
        }

        public async Task<IActionResult> Reports(DateTime? startDate, DateTime? endDate)
        {
            var sellerId = _userManager.GetUserId(User);
            var start = startDate ?? DateTime.Now.AddDays(-30).Date;
            var end = (endDate ?? DateTime.Now.Date).Date.AddDays(1).AddTicks(-1);

            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.SellerId == sellerId && oi.Order!.OrderDate >= start && oi.Order.OrderDate <= end)
                .ToListAsync();

            var model = new ReportViewModel
            {
                StartDate = start,
                EndDate = end,
                TotalSales = orderItems.Sum(oi => oi.TotalPrice),
                TotalOrders = orderItems.Select(oi => oi.OrderId).Distinct().Count(),
                TotalProfit = orderItems.Sum(oi => oi.TotalPrice),
                MonthlySales = orderItems
                    .GroupBy(oi => oi.Order!.OrderDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailySales { Label = g.Key.ToString("MMM d"), Amount = g.Sum(oi => oi.TotalPrice) })
                    .ToList()
            };

            ViewBag.ActiveNav = "Reports";
            return View(model);
        }
    }
}
