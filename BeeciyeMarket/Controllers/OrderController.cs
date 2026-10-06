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

            var model = await NewCheckoutModelAsync(items);

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

            model.Items = cartItems;
            model.BuyNowProductId = null;

            var order = await PlaceOrderAsync(model, userId);
            if (order == null)
            {
                ViewBag.ActiveNav = "Cart";
                return View(model);
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive || product.Quantity < 1)
            {
                return NotFound();
            }

            quantity = Math.Max(1, Math.Min(quantity, product.Quantity));

            var model = await NewCheckoutModelAsync(new List<CartItem>
            {
                new CartItem { ProductId = product.Id, Product = product, Quantity = quantity }
            });
            model.BuyNowProductId = product.Id;
            model.BuyNowQuantity = quantity;

            ViewBag.ActiveNav = "Browse";
            return View(nameof(Checkout), model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(CheckoutViewModel model)
        {
            var product = model.BuyNowProductId.HasValue
                ? await _context.Products.FindAsync(model.BuyNowProductId.Value)
                : null;
            if (product == null || !product.IsActive || product.Quantity < 1)
            {
                return NotFound();
            }

            model.BuyNowQuantity = Math.Max(1, model.BuyNowQuantity);
            model.Items = new List<CartItem>
            {
                new CartItem { ProductId = product.Id, Product = product, Quantity = model.BuyNowQuantity }
            };

            var userId = _userManager.GetUserId(User);
            var order = await PlaceOrderAsync(model, userId);
            if (order == null)
            {
                ViewBag.ActiveNav = "Browse";
                return View(nameof(Checkout), model);
            }

            if (userId == null)
            {
                // Lets the guest see their own confirmation page, and only that one
                TempData["GuestOrderId"] = order.Id.ToString();
            }

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        private async Task<CheckoutViewModel> NewCheckoutModelAsync(List<CartItem> items)
        {
            var user = await _userManager.GetUserAsync(User);
            return new CheckoutViewModel
            {
                Items = items,
                ShippingFullName = user?.FullName ?? string.Empty,
                ShippingPhone = user?.PhoneNumber ?? string.Empty,
                ShippingCity = user?.City,
                ShippingAddress = user?.Address
            };
        }

        // Validates stock, creates the order with its delivery details and payment record.
        // Returns null (with ModelState errors) when the order can't be placed.
        private async Task<Order?> PlaceOrderAsync(CheckoutViewModel model, string? userId)
        {
            if (!ModelState.IsValid)
            {
                return null;
            }

            foreach (var item in model.Items)
            {
                if (item.Product == null || item.Quantity > item.Product.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"'{item.Product?.Name}' no longer has enough stock.");
                }
            }

            if (!ModelState.IsValid)
            {
                return null;
            }

            var isPickup = model.DeliveryMethod == DeliveryMethod.Pickup;
            var order = new Order
            {
                CustomerId = userId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                ShippingFullName = model.ShippingFullName,
                ShippingPhone = model.ShippingPhone,
                DeliveryMethod = model.DeliveryMethod,
                DeliveryFee = model.DeliveryFee,
                ShippingCity = isPickup ? null : model.ShippingCity,
                ShippingDistrict = isPickup ? null : model.ShippingDistrict,
                ShippingAddress = isPickup ? string.Empty : model.ShippingAddress ?? string.Empty,
                DeliveryNotes = model.DeliveryNotes,
                PaymentMethod = model.PaymentMethod,
                TotalPrice = model.Total
            };

            foreach (var item in model.Items)
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

            var isMobileMoney = model.PaymentMethod == PaymentMethod.MobileMoney;
            order.Payments.Add(new Payment
            {
                Amount = order.TotalPrice,
                PaymentMethod = order.PaymentMethod,
                PaymentDate = DateTime.Now,
                Status = isMobileMoney ? PaymentStatus.Paid : PaymentStatus.Pending,
                Provider = isMobileMoney ? model.Provider : null,
                PayerPhone = isMobileMoney ? model.PayerPhone : null,
                TransactionReference = isMobileMoney ? model.TransactionReference?.Trim() : null
            });

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await _notifications.NotifyOrderPlacedAsync(order.Id);

            return order;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null && TempData.Peek("GuestOrderId") as string != id.ToString())
            {
                return NotFound();
            }

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
