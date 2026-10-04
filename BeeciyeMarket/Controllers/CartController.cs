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
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.DateAdded)
                .ToListAsync();

            ViewBag.ActiveNav = "Cart";
            return View(new CartViewModel { Items = items });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
            {
                return NotFound();
            }

            quantity = Math.Max(1, Math.Min(quantity, product.Quantity));

            var userId = _userManager.GetUserId(User)!;
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existing != null)
            {
                existing.Quantity = Math.Min(existing.Quantity + quantity, product.Quantity);
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    DateAdded = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.CartItems.Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (item == null)
            {
                return NotFound();
            }

            item.Quantity = Math.Max(1, Math.Min(quantity, item.Product!.Quantity));
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
