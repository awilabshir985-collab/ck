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
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly INotificationService _notifications;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, INotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _notifications = notifications;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalSellers = await _context.Products.Select(p => p.SellerId).Distinct().CountAsync(),
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalSales = await _context.Orders.SumAsync(o => (decimal?)o.TotalPrice) ?? 0,
                RecentProducts = await _context.Products
                    .Include(p => p.Category).Include(p => p.Seller)
                    .OrderByDescending(p => p.DateAdded).Take(5).ToListAsync(),
                RecentOrders = await _context.Orders
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderDate).Take(5).ToListAsync()
            };

            ViewBag.ActiveNav = "Dashboard";
            return View(model);
        }

        public async Task<IActionResult> Users(string? role)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .Any(x => x.UserId == u.Id && x.Name == role));
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            var roleNames = new Dictionary<string, List<string>>();

            foreach (var user in users)
            {
                var roles = await (from ur in _context.UserRoles
                                    join r in _context.Roles on ur.RoleId equals r.Id
                                    where ur.UserId == user.Id
                                    select r.Name!).ToListAsync();
                roleNames[user.Id] = roles;
            }

            ViewBag.UserRoles = roleNames;
            ViewBag.RoleFilter = role;
            ViewBag.ActiveNav = "Users";
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
            ViewBag.IsAdmin = isAdmin;
            ViewBag.ActiveNav = "Users";
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, ApplicationUser model, bool isAdmin)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.City = model.City;
            user.Address = model.Address;
            await _context.SaveChangesAsync();

            var currentlyAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
            if (isAdmin && !currentlyAdmin)
            {
                await _userManager.AddToRoleAsync(user, Roles.Admin);
            }
            else if (!isAdmin && currentlyAdmin)
            {
                var adminCount = (await _userManager.GetUsersInRoleAsync(Roles.Admin)).Count;
                if (adminCount > 1)
                {
                    await _userManager.RemoveFromRoleAsync(user, Roles.Admin);
                }
                else
                {
                    TempData["Error"] = "Cannot remove the last remaining admin.";
                }
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewBag.ActiveNav = "Users";
            return View(new ApplicationUser());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(ApplicationUser model, string password, bool isAdmin)
        {
            ModelState.Remove(nameof(ApplicationUser.UserName));

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(nameof(ApplicationUser.Email), "Email is required.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "Password must be at least 6 characters.");
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    City = model.City,
                    Address = model.Address,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, isAdmin ? Roles.Admin : Roles.User);
                    return RedirectToAction(nameof(Users));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.ActiveNav = "Users";
            return View(model);
        }

        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Category).Include(p => p.Seller)
                .OrderByDescending(p => p.DateAdded)
                .ToListAsync();

            ViewBag.ActiveNav = "Products";
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Sellers = await _context.Users.OrderBy(u => u.FullName).ToListAsync();
            ViewBag.ActiveNav = "Products";
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                product.DateAdded = DateTime.Now;
                product.IsActive = true;
                product.InitialQuantity = product.Quantity;

                if (product.ImageFile != null)
                {
                    product.ImagePath = await SaveImageAsync(product.ImageFile);
                    product.Images.Add(new ProductImage { ImagePath = product.ImagePath, DisplayOrder = 0 });
                }

                if (product.ImageFiles != null)
                {
                    var order = product.Images.Count;
                    foreach (var file in product.ImageFiles)
                    {
                        var path = await SaveImageAsync(file);
                        product.Images.Add(new ProductImage { ImagePath = path, DisplayOrder = order++ });
                    }
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Sellers = await _context.Users.OrderBy(u => u.FullName).ToListAsync();
            ViewBag.ActiveNav = "Products";
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProductActive(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.IsActive = !product.IsActive;
            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(product.SellerId,
                product.IsActive ? "Product activated" : "Product deactivated",
                product.IsActive
                    ? $"Your product '{product.Name}' is now visible on the market."
                    : $"Your product '{product.Name}' was hidden from the market by an administrator.",
                "/Product/MyProducts",
                NotificationType.Product);

            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.ActiveNav = "Products";
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            var existing = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Product.SellerId));

            if (ModelState.IsValid)
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.InitialQuantity = product.Quantity;
                existing.Quantity = product.Quantity;
                existing.Condition = product.Condition;
                existing.CategoryId = product.CategoryId;
                existing.IsActive = product.IsActive;

                if (product.ImageFile != null)
                {
                    existing.ImagePath = await SaveImageAsync(product.ImageFile);
                    existing.Images.Add(new ProductImage { ImagePath = existing.ImagePath, DisplayOrder = existing.Images.Count });
                }

                if (product.ImageFiles != null)
                {
                    var order = existing.Images.Count;
                    foreach (var file in product.ImageFiles)
                    {
                        var path = await SaveImageAsync(file);
                        existing.Images.Add(new ProductImage { ImagePath = path, DisplayOrder = order++ });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.ActiveNav = "Products";
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductImage(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null)
            {
                return NotFound();
            }

            var productId = image.ProductId;
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(EditProduct), new { id = productId });
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/products/{fileName}";
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new { Category = c, ProductCount = c.Products.Count })
                .ToListAsync();

            ViewBag.ProductCounts = categories.ToDictionary(x => x.Category.Id, x => x.ProductCount);
            ViewBag.ActiveNav = "Categories";
            return View(categories.Select(x => x.Category).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name, string? iconClass)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _context.Categories.Add(new Category { Name = name.Trim(), IconClass = iconClass });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            ViewBag.ActiveNav = "Categories";
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string name, string? description, string? iconClass)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                category.Name = name.Trim();
                category.Description = description;
                category.IconClass = iconClass;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            if (category.Products.Any())
            {
                TempData["Error"] = "Cannot delete a category that still has products.";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.ActiveNav = "Orders";
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            var previousStatus = order.Status;
            order.Status = status;
            await _context.SaveChangesAsync();

            if (previousStatus != status)
            {
                var orderNo = $"#{order.Id:D4}";
                var customerMessage = status switch
                {
                    OrderStatus.Confirmed => $"Your order {orderNo} has been confirmed and is being prepared.",
                    OrderStatus.Completed => $"Your order {orderNo} has been completed. Thank you for shopping with Beeciye!",
                    OrderStatus.Cancelled => $"Your order {orderNo} has been cancelled.",
                    _ => $"Your order {orderNo} status changed to {status}."
                };

                await _notifications.NotifyAsync(order.CustomerId,
                    $"Order {status.ToString().ToLower()}",
                    customerMessage,
                    $"/Order/Details/{order.Id}",
                    NotificationType.Order);

                await _notifications.NotifyManyAsync(order.OrderItems.Select(oi => oi.SellerId),
                    $"Sale {status.ToString().ToLower()}",
                    $"Order {orderNo} containing your product(s) is now {status}.",
                    "/Order/Sales",
                    NotificationType.Order);
            }

            return RedirectToAction(nameof(Orders));
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Seller)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.ActiveNav = "Orders";
            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> CreateOrder()
        {
            ViewBag.Customers = await _context.Users.OrderBy(u => u.FullName).ToListAsync();
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            ViewBag.ActiveNav = "Orders";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(string customerId, string shippingFullName, string shippingPhone, string shippingAddress,
            PaymentMethod paymentMethod, int[] productId, int[] quantity)
        {
            if (string.IsNullOrEmpty(customerId) || productId == null || productId.Length == 0)
            {
                TempData["Error"] = "Select a customer and at least one product.";
                return RedirectToAction(nameof(CreateOrder));
            }

            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                ShippingFullName = shippingFullName,
                ShippingPhone = shippingPhone,
                ShippingAddress = shippingAddress,
                PaymentMethod = paymentMethod
            };

            decimal total = 0;
            for (int i = 0; i < productId.Length; i++)
            {
                var qty = i < quantity.Length && quantity[i] > 0 ? quantity[i] : 1;
                var product = await _context.Products.FindAsync(productId[i]);
                if (product == null)
                {
                    continue;
                }

                var lineTotal = product.UnitPrice * qty;
                total += lineTotal;

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    SellerId = product.SellerId,
                    Quantity = qty,
                    UnitPrice = product.UnitPrice,
                    TotalPrice = lineTotal
                });

                product.Quantity = Math.Max(0, product.Quantity - qty);
                if (product.Quantity <= 0)
                {
                    product.IsActive = false;
                }
            }

            order.TotalPrice = total;
            _context.Orders.Add(order);
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

            return RedirectToAction(nameof(OrderDetails), new { id = order.Id });
        }

        public async Task<IActionResult> Payments()
        {
            var payments = await _context.Payments
                .Include(p => p.Order).ThenInclude(o => o!.Customer)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.ActiveNav = "Payments";
            return View(payments);
        }

        [HttpGet]
        public async Task<IActionResult> CreatePayment()
        {
            ViewBag.Orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Payments)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            ViewBag.ActiveNav = "Payments";
            return View(new Payment());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(int orderId, decimal amount, PaymentMethod paymentMethod, PaymentStatus status, string? transactionReference)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return NotFound();
            }

            _context.Payments.Add(new Payment
            {
                OrderId = orderId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                PaymentDate = DateTime.Now,
                Status = status,
                TransactionReference = transactionReference
            });
            await _context.SaveChangesAsync();

            var paymentMessage = status switch
            {
                PaymentStatus.Paid => $"A payment of ${amount:0.00} for order #{order.Id:D4} was received.",
                PaymentStatus.Failed => $"A payment of ${amount:0.00} for order #{order.Id:D4} failed. Please try again or contact support.",
                _ => $"A payment of ${amount:0.00} for order #{order.Id:D4} is pending."
            };
            await _notifications.NotifyAsync(order.CustomerId,
                $"Payment {status.ToString().ToLower()}",
                paymentMessage,
                $"/Order/Details/{order.Id}",
                NotificationType.Payment);

            return RedirectToAction(nameof(Payments));
        }

        public async Task<IActionResult> Reports(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now.AddDays(-30).Date;
            var end = (endDate ?? DateTime.Now.Date).Date.AddDays(1).AddTicks(-1);

            var orders = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .ToListAsync();

            var model = new ReportViewModel
            {
                StartDate = start,
                EndDate = end,
                TotalSales = orders.Sum(o => o.TotalPrice),
                TotalOrders = orders.Count,
                TotalProfit = orders.Sum(o => o.TotalPrice),
                MonthlySales = orders
                    .GroupBy(o => o.OrderDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailySales { Label = g.Key.ToString("MMM d"), Amount = g.Sum(o => o.TotalPrice) })
                    .ToList()
            };

            ViewBag.ActiveNav = "Reports";
            return View(model);
        }
    }
}
