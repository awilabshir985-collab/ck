using BeeciyeMarket.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BeeciyeMarket.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Migrations are SQL Server-specific; on PostgreSQL build the schema straight from the model
            if (context.Database.IsNpgsql())
                await context.Database.EnsureCreatedAsync();
            else
                await context.Database.MigrateAsync();

            // 1. Roles
            foreach (var role in new[] { Roles.Admin, Roles.User })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Admin user
            var adminEmail = "admin@beeciye.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Beeciye Administrator",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                }
            }

            // 3. Demo user
            var demoEmail = "Abdikafi4455@gmail.com";
            var demoUser = await userManager.FindByEmailAsync(demoEmail);
            if (demoUser == null)
            {
                demoUser = new ApplicationUser
                {
                    UserName = demoEmail,
                    Email = demoEmail,
                    FullName = "Ahmed Hassan",
                    City = "Garowe",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };
                var result = await userManager.CreateAsync(demoUser, "User@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(demoUser, Roles.User);
                }
            }

            // 4. Categories
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Electronics", IconClass = "bi bi-tv" },
                    new Category { Name = "Phones", IconClass = "bi bi-phone" },
                    new Category { Name = "Computers", IconClass = "bi bi-laptop" },
                    new Category { Name = "Clothes", IconClass = "bi bi-bag" },
                    new Category { Name = "Shoes", IconClass = "bi bi-boot" },
                    new Category { Name = "Furniture", IconClass = "bi bi-lamp" },
                    new Category { Name = "Vehicles", IconClass = "bi bi-car-front" },
                    new Category { Name = "Other", IconClass = "bi bi-box-seam" }
                );
                context.SaveChanges();
            }

            // 5. Sample products (only if none exist yet)
            if (!context.Products.Any() && demoUser != null)
            {
                var phones = context.Categories.First(c => c.Name == "Phones");
                var computers = context.Categories.First(c => c.Name == "Computers");
                var clothes = context.Categories.First(c => c.Name == "Clothes");
                var furniture = context.Categories.First(c => c.Name == "Furniture");

                context.Products.AddRange(
                    new Product
                    {
                        Name = "iPhone 12",
                        Description = "iPhone 12 in good condition. 128GB storage. Comes with charger and cable. No scratches. Works perfectly.",
                        Price = 250,
                        Quantity = 1,
                        Condition = ProductCondition.Good,
                        CategoryId = phones.Id,
                        SellerId = demoUser.Id,
                        DateAdded = DateTime.Now
                    },
                    new Product
                    {
                        Name = "HP Laptop",
                        Description = "HP Laptop i5, 8GB RAM, 256GB SSD. Good condition.",
                        Price = 350,
                        Quantity = 5,
                        Condition = ProductCondition.Good,
                        CategoryId = computers.Id,
                        SellerId = demoUser.Id,
                        DateAdded = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Men's Jacket",
                        Description = "Lightly used men's jacket, size L.",
                        Price = 20,
                        Quantity = 10,
                        Condition = ProductCondition.Used,
                        CategoryId = clothes.Id,
                        SellerId = demoUser.Id,
                        DateAdded = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Sofa",
                        Description = "Comfortable 3-seater sofa in great condition.",
                        Price = 120,
                        Quantity = 2,
                        Condition = ProductCondition.Good,
                        CategoryId = furniture.Id,
                        SellerId = demoUser.Id,
                        DateAdded = DateTime.Now
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
