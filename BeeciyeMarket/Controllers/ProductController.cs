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
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly INotificationService _notifications;

        public ProductController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, INotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _notifications = notifications;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
        {
            const int pageSize = 8;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Where(p => p.IsActive && !p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Max(1, page);

            var products = await query
                .OrderByDescending(p => p.DateAdded)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new ProductListViewModel
            {
                Products = products,
                Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                SearchTerm = search,
                SelectedCategoryId = categoryId,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            ViewBag.ActiveNav = "Browse";
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.ActiveNav = "Browse";
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> MyProducts()
        {
            var userId = _userManager.GetUserId(User);
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.SellerId == userId && !p.IsDeleted)
                .OrderByDescending(p => p.DateAdded)
                .ToListAsync();

            ViewBag.ActiveNav = "MyProducts";
            return View(products);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.ActiveNav = "Create";
            return View(new Product());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            ModelState.Remove(nameof(Product.SellerId));

            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            if (isAuthenticated)
            {
                ModelState.Remove(nameof(Product.GuestName));
                ModelState.Remove(nameof(Product.GuestPhone));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(product.GuestName))
                {
                    ModelState.AddModelError(nameof(Product.GuestName), "Your name is required.");
                }
                if (string.IsNullOrWhiteSpace(product.GuestPhone))
                {
                    ModelState.AddModelError(nameof(Product.GuestPhone), "Your phone number is required.");
                }
            }

            if (ModelState.IsValid)
            {
                if (isAuthenticated)
                {
                    product.SellerId = _userManager.GetUserId(User);
                    product.GuestName = null;
                    product.GuestPhone = null;
                }
                else
                {
                    product.SellerId = null;
                }

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

                var listedBy = isAuthenticated ? User.Identity!.Name : $"{product.GuestName} (guest)";
                await _notifications.NotifyAdminsAsync(
                    "New product listed",
                    $"'{product.Name}' was listed for ${product.UnitPrice:0.00} by {listedBy}.",
                    $"/Product/Details/{product.Id}",
                    NotificationType.Product);

                if (!isAuthenticated)
                {
                    TempData["Success"] = "Alaabtaada waa la diiwaan geliyay! Waxay hadda ka muuqan doontaa suuqa.";
                    return RedirectToAction(nameof(Details), new { id = product.Id });
                }

                return RedirectToAction(nameof(MyProducts));
            }

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.ActiveNav = "Create";
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null || product.SellerId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.ActiveNav = "MyProducts";
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            var existing = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null || existing.SellerId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Product.SellerId));

            if (ModelState.IsValid)
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.Quantity = product.Quantity;
                existing.InitialQuantity = product.Quantity;
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
                return RedirectToAction(nameof(MyProducts));
            }

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.ActiveNav = "MyProducts";
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null || product.SellerId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            var hasOrderHistory = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrderHistory)
            {
                product.IsDeleted = true;
                product.IsActive = false;
            }
            else
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyProducts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var image = await _context.ProductImages.Include(i => i.Product).FirstOrDefaultAsync(i => i.Id == id);
            if (image == null || image.Product == null || image.Product.SellerId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            var productId = image.ProductId;
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = productId });
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
    }
}
