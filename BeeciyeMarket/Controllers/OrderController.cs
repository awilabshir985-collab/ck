using BeeciyeMarket.Data;
using BeeciyeMarket.Models;
using BeeciyeMarket.Models.ViewModels;
using BeeciyeMarket.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeeciyeMarket.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notifications;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            var model = new CheckoutViewModel
            {
                Items = items,
                ShippingFullName = user?.FullName ?? string.Empty,
                ShippingPhone = user?.PhoneNumber ?? string.Empty,
                ShippingAddress = user?.Address ?? string.Empty
            };

            ViewBag.ActiveNav = "Cart";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = _userManager.GetUserId(User)!;
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                model.Items = cartItems;
                return View(model);
            }

            foreach (var item in cartItems)
            {
                if (item.Product == null || item.Quantity > item.Product.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"'{item.Product?.Name}' no longer has enough stock.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.Items = cartItems;
                return View(model);
            }

            var order = new Order
            {
                CustomerId = userId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                ShippingFullName = model.ShippingFullName,
                ShippingPhone = model.ShippingPhone,
                ShippingAddress = model.ShippingAddress,
                PaymentMethod = model.PaymentMethod,
                TotalPrice = cartItems.Sum(i => i.Product!.UnitPrice * i.Quantity)
            };

            foreach (var item in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    SellerId = item.Product!.SellerId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.UnitPrice,
                    TotalPrice = item.Product.UnitPrice * item.Quantity
                });

                item.Product.Quantity -= item.Quantity;
                if (item.Product.Quantity <= 0)
                {
                    item.Product.IsActive = false;
                }
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            order.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalPrice,
                PaymentMethod = order.PaymentMethod,
                PaymentDate = DateTime.Now,
                Status = order.PaymentMethod == PaymentMethod.CashOnDelivery ? PaymentStatus.Pending : PaymentStatus.Paid
            });
            await _context.SaveChangesAsync();

            await _notifications.NotifyOrderPlacedAsync(order.Id);

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1, string? guestName = null, string? guestPhone = null, string? guestAddress = null)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive || product.Quantity < 1)
            {
                return NotFound();
            }

            quantity = Math.Max(1, Math.Min(quantity, product.Quantity));

            var isAuthenticated = User.Identity?.IsAuthenticated == true;

            if (!isAuthenticated)
            {
                if (string.IsNullOrWhiteSpace(guestName) || string.IsNullOrWhiteSpace(guestPhone) || string.IsNullOrWhiteSpace(guestAddress))
                {
                    ModelState.AddModelError(string.Empty, "Please provide your name, phone number and address to complete the purchase.");
                    return RedirectToAction("Details", "Product", new { id = productId });
                }
            }

            string? userId = null;
            ApplicationUser? user = null;
            if (isAuthenticated)
            {
                userId = _userManager.GetUserId(User)!;
                user = await _userManager.GetUserAsync(User);
            }

            var order = new Order
            {
                CustomerId = userId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                ShippingFullName = isAuthenticated ? (user?.FullName ?? string.Empty) : guestName!,
                ShippingPhone = isAuthenticated ? (user?.PhoneNumber ?? string.Empty) : guestPhone!,
                ShippingAddress = isAuthenticated ? (user?.Address ?? user?.City ?? string.Empty) : guestAddress!,
                PaymentMethod = PaymentMethod.CashOnDelivery,
                TotalPrice = product.UnitPrice * quantity
            };

            order.OrderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                SellerId = product.SellerId,
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
                TotalPrice = product.UnitPrice * quantity
            });

            product.Quantity -= quantity;
            if (product.Quantity <= 0)
            {
                product.IsActive = false;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            order.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalPrice,
                PaymentMethod = order.PaymentMethod,
                PaymentDate = DateTime.Now,
                Status = PaymentStatus.Pending
            });
            await _context.SaveChangesAsync();

            await _notifications.NotifyOrderPlacedAsync(order.Id);

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.ActiveNav = "MyOrders";
            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Index(OrderStatus? status)
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == userId);

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.ActiveNav = "MyOrders";
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.ActiveNav = "MyOrders";
            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Sales()
        {
            var sellerId = _userManager.GetUserId(User);
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.SellerId == sellerId)
                .OrderByDescending(oi => oi.Order!.OrderDate)
                .ToListAsync();

            ViewBag.ActiveNav = "Sales";
            return View(orderItems);
        }
    }
}
