using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ggChick.Models;
using ggChick.Models.Enums;

namespace ggChick.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure database is created/migrated
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Admin User
            var adminEmail = "admin@ggcliks.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FullName = "GGCLIKS System Admin",
                    EmailConfirmed = true,
                    Address = "100 Innovation Blvd, Tech City",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                };

                var createAdmin = await userManager.CreateAsync(adminUser, "Admin@123");
                if (createAdmin.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Regular Demo User
            var demoEmail = "user@ggcliks.com";
            var demoUser = await userManager.FindByEmailAsync(demoEmail);
            if (demoUser == null)
            {
                demoUser = new ApplicationUser
                {
                    UserName = "customer",
                    Email = demoEmail,
                    FullName = "Jane Doe",
                    EmailConfirmed = true,
                    Address = "45 Elm Street, Springfield",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                };

                var createUser = await userManager.CreateAsync(demoUser, "User@123");
                if (createUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(demoUser, "User");
                }
            }

            // 4. Clean up any obsolete categories & products, seed exact target categories: Tshirt, cardigans, hoodies, pants
            var targetCategoryDefinitions = new List<(string Name, string Description)>
            {
                ("Tshirt", "Premium streetwear graphic tees, oversized washed cotton t-shirts, and everyday minimal basics."),
                ("cardigans", "Cozy knitwear, oversized button-ups, ribbed cardigans, and layered outerwear."),
                ("hoodies", "Heavyweight fleece, streetwear pullover hoodies, and zip-ups."),
                ("pants", "Relaxed cargo pants, baggy trousers, streetwear bottoms, and everyday casual pants.")
            };

            var existingCategories = await context.Categories.ToListAsync();
            var targetNames = targetCategoryDefinitions.Select(t => t.Name).ToList();

            // Detect if catalog has obsolete categories (e.g. Electronics, Fashion, Accessories, Home & Living)
            var obsoleteCategories = existingCategories
                .Where(c => !targetNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (obsoleteCategories.Any())
            {
                // Delete dependent entities for obsolete categories
                var obsoleteCatIds = obsoleteCategories.Select(c => c.Id).ToList();
                var obsoleteProducts = await context.Products
                    .Where(p => obsoleteCatIds.Contains(p.CategoryId))
                    .Include(p => p.Images)
                    .Include(p => p.Reviews)
                    .Include(p => p.Comments)
                    .Include(p => p.OrderItems)
                    .ToListAsync();

                foreach (var product in obsoleteProducts)
                {
                    context.OrderItems.RemoveRange(product.OrderItems);
                    context.Reviews.RemoveRange(product.Reviews);
                    context.Comments.RemoveRange(product.Comments);
                    context.ProductImages.RemoveRange(product.Images);
                }
                context.Products.RemoveRange(obsoleteProducts);
                context.Categories.RemoveRange(obsoleteCategories);
                await context.SaveChangesAsync();

                // Refresh existing categories
                existingCategories = await context.Categories.ToListAsync();
            }

            // Ensure all 4 target categories exist
            foreach (var target in targetCategoryDefinitions)
            {
                var existing = existingCategories.FirstOrDefault(c => c.Name.Equals(target.Name, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    var newCat = new Category
                    {
                        Name = target.Name,
                        Description = target.Description,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Categories.Add(newCat);
                }
            }
            await context.SaveChangesAsync();

            // Refresh categories lookup
            var categoryMap = await context.Categories.ToDictionaryAsync(c => c.Name.ToLowerInvariant(), c => c);

            // 5. Seed Products if empty or if new categories lack products
            var totalProducts = await context.Products.CountAsync();
            if (totalProducts < 10)
            {
                // Remove any existing leftover demo products to build the clean fashion catalog
                context.OrderItems.RemoveRange(context.OrderItems);
                context.Orders.RemoveRange(context.Orders);
                context.Comments.RemoveRange(context.Comments);
                context.Reviews.RemoveRange(context.Reviews);
                context.ProductImages.RemoveRange(context.ProductImages);
                context.Products.RemoveRange(context.Products);
                await context.SaveChangesAsync();

                var tshirtCat = categoryMap["tshirt"];
                var cardigansCat = categoryMap["cardigans"];
                var hoodiesCat = categoryMap["hoodies"];
                var pantsCat = categoryMap["pants"];

                var products = new List<Product>
                {
                    // --- TSHIRT PRODUCTS ---
                    new Product
                    {
                        Name = "AW22 Heavyweight Vintage Graphic Tee",
                        Description = "Premium heavyweight 280 GSM cotton vintage washed t-shirt featuring exclusive AW22 street artwork, drop-shoulder silhouette, and reinforced ribbed collar.",
                        Price = 44.99m,
                        StockQuantity = 60,
                        CategoryId = tshirtCat.Id,
                        MainImageUrl = "/images/products/AW22.jpeg",
                        CreatedAt = DateTime.UtcNow.AddDays(-28),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/AW22.jpeg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/t1.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Dope Chef Streetwear Boxy T-Shirt",
                        Description = "Iconic streetwear boxy cut tee crafted from 100% breathable organic ring-spun cotton with high-density chest print and custom woven hem label.",
                        Price = 48.50m,
                        StockQuantity = 50,
                        CategoryId = tshirtCat.Id,
                        MainImageUrl = "/images/products/Dope Chef.jpeg",
                        CreatedAt = DateTime.UtcNow.AddDays(-25),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/Dope Chef.jpeg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/t2.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Acid Wash Distressed Oversized Tee",
                        Description = "Hand-treated mineral wash t-shirt with subtle distressed accents along the neckline and sleeves for an authentic vintage worn-in aesthetic.",
                        Price = 39.99m,
                        StockQuantity = 85,
                        CategoryId = tshirtCat.Id,
                        MainImageUrl = "/images/products/t3.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-22),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/t3.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/t4.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Essential Minimalist Boxy Black Tee",
                        Description = "Clean understated silhouette cut from ultra-soft combed cotton. Features seamless tubular body construction and durable double-needle stitching.",
                        Price = 36.00m,
                        StockQuantity = 90,
                        CategoryId = tshirtCat.Id,
                        MainImageUrl = "/images/products/t4.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-20),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/t4.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/b.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Cyber Graphic Heavyweight Washed Tee",
                        Description = "Futuristic typography and abstract street graphic screen-printed with fade-resistant inks on heavyweight stone-washed cotton.",
                        Price = 46.00m,
                        StockQuantity = 40,
                        CategoryId = tshirtCat.Id,
                        MainImageUrl = "/images/products/t6.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-16),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/t6.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/t5.jpeg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Earth Tone Drop-Shoulder Relaxed Tee",
                        Description = "Modern earthy neutral hue with relaxed drop-shoulder cut, wider sleeve opening, and breathable relaxed drape for effortless layering.",
                        Price = 42.00m,
                        StockQuantity = 70,
                        CategoryId = tshirtCat.Id,
                        MainImageUrl = "/images/products/t8.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-12),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/t8.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/t7.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Monochrome Heritage Crewneck Tee",
                        Description = "Everyday staple tee offering the perfect balance between structure and comfort. Pre-shrunk finish ensures it retains its shape wash after wash.",
                        Price = 34.50m,
                        StockQuantity = 100,
                        CategoryId = tshirtCat.Id,
                        MainImageUrl = "/images/products/t10.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/t10.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/t9.jpg", IsMain = false },
                            new ProductImage { ImageUrl = "/images/products/t11.jpg", IsMain = false }
                        }
                    },

                    // --- CARDIGANS PRODUCTS ---
                    new Product
                    {
                        Name = "Chunky Oversized Mohair Knit Cardigan",
                        Description = "Ultra-plush mohair-wool blend cardigan with oversized horn buttons, relaxed dropped shoulders, and ribbed cuffs for maximum warmth and luxury.",
                        Price = 98.00m,
                        StockQuantity = 35,
                        CategoryId = cardigansCat.Id,
                        MainImageUrl = "/images/products/cardigan.webp",
                        CreatedAt = DateTime.UtcNow.AddDays(-27),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/cardigan.webp", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/cardigan2.webp", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Nordic Geometric Brushed Wool Cardigan",
                        Description = "Heritage-inspired brushed wool knit featuring intricate geometric patterns, deep V-neckline, and cozy relaxed drape.",
                        Price = 115.00m,
                        StockQuantity = 28,
                        CategoryId = cardigansCat.Id,
                        MainImageUrl = "/images/products/cardigan2.webp",
                        CreatedAt = DateTime.UtcNow.AddDays(-24),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/cardigan2.webp", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/car2.jpeg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Vintage Ribbed Textured Button Cardigan",
                        Description = "Classic ribbed knit button-front cardigan crafted with dense cotton-blend yarn. Features dual front patch pockets and tailored cuffs.",
                        Price = 89.00m,
                        StockQuantity = 45,
                        CategoryId = cardigansCat.Id,
                        MainImageUrl = "/images/products/c.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-21),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/c.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/c.jpeg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Relaxed V-Neck Textured Street Cardigan",
                        Description = "Contemporary streetwear knit cardigan featuring a textured waffle weave, tortoise buttons, and an effortlessly relaxed silhouette.",
                        Price = 92.50m,
                        StockQuantity = 32,
                        CategoryId = cardigansCat.Id,
                        MainImageUrl = "/images/products/car2.jpeg",
                        CreatedAt = DateTime.UtcNow.AddDays(-18),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/car2.jpeg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/c3.jpeg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Heritage Striped Preppy Cardigan",
                        Description = "Collegiate aesthetic with bold contrast striping, ribbed hemline, and breathable medium-weight cotton-wool blend.",
                        Price = 105.00m,
                        StockQuantity = 25,
                        CategoryId = cardigansCat.Id,
                        MainImageUrl = "/images/products/c4.jpeg",
                        CreatedAt = DateTime.UtcNow.AddDays(-15),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/c4.jpeg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/c5.jpeg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Minimalist Earth Tone Button Knit Cardigan",
                        Description = "Neutral minimalist knitwear piece designed for versatile layering over tees or button-down shirts. Features smooth horn buttons and clean hems.",
                        Price = 85.00m,
                        StockQuantity = 40,
                        CategoryId = cardigansCat.Id,
                        MainImageUrl = "/images/products/c6.jpeg",
                        CreatedAt = DateTime.UtcNow.AddDays(-11),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/c6.jpeg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/c7.jpeg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Subtle Jacquard Pattern Cozy Cardigan",
                        Description = "Sophisticated tonal jacquard weave knit cardigan with premium hand-feel and comfortable relaxed silhouette.",
                        Price = 96.00m,
                        StockQuantity = 30,
                        CategoryId = cardigansCat.Id,
                        MainImageUrl = "/images/products/c8.jpeg",
                        CreatedAt = DateTime.UtcNow.AddDays(-8),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/c8.jpeg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/c9.jpeg", IsMain = false },
                            new ProductImage { ImageUrl = "/images/products/c10.jpeg", IsMain = false },
                            new ProductImage { ImageUrl = "/images/products/c11.jpeg", IsMain = false }
                        }
                    },

                    // --- HOODIES PRODUCTS ---
                    new Product
                    {
                        Name = "Classic Heavyweight French Terry Hoodie",
                        Description = "Premium 450 GSM French terry fleece hoodie with double-layer hood, thick ribbing, matte metal eyelets, and kangaroo pocket.",
                        Price = 78.00m,
                        StockQuantity = 55,
                        CategoryId = hoodiesCat.Id,
                        MainImageUrl = "/images/products/normal hoodie.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-29),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/normal hoodie.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/h.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Streetwear Acid Wash Pullover Hoodie",
                        Description = "Unique mineral wash finish giving each hoodie a one-of-a-kind vintage fade. Designed with an oversized streetwear cut and raw hem detailing.",
                        Price = 88.00m,
                        StockQuantity = 42,
                        CategoryId = hoodiesCat.Id,
                        MainImageUrl = "/images/products/h1.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-26),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/h1.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/h2.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Thermal Drop-Shoulder Boxy Hoodie",
                        Description = "Cozy fleece-lined heavyweight pullover with drop-shoulder tailoring, seamless hood construction, and reinforced ribbing.",
                        Price = 84.50m,
                        StockQuantity = 60,
                        CategoryId = hoodiesCat.Id,
                        MainImageUrl = "/images/products/h2.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-23),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/h2.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/ha.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Embroidered Graphic Boxy Fleece Hoodie",
                        Description = "High-density chest embroidery and sleeve detail on custom-dyed premium fleece. Features hidden interior phone pocket inside the kangaroo pouch.",
                        Price = 92.00m,
                        StockQuantity = 36,
                        CategoryId = hoodiesCat.Id,
                        MainImageUrl = "/images/products/h3.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-19),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/h3.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/h4.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Vintage Washed Slate Pullover Hoodie",
                        Description = "Subtle garment-dyed slate hue with brushed interior fleece for cloud-like softness and an effortless drape.",
                        Price = 79.99m,
                        StockQuantity = 48,
                        CategoryId = hoodiesCat.Id,
                        MainImageUrl = "/images/products/h4.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-14),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/h4.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/h5.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Signature Minimalist Oversized Hoodie",
                        Description = "Understated luxury aesthetic with zero exterior branding, clean lines, and heavy dense fabric that holds its structured shape.",
                        Price = 86.00m,
                        StockQuantity = 52,
                        CategoryId = hoodiesCat.Id,
                        MainImageUrl = "/images/products/h7.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-9),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/h7.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/h6.jpg", IsMain = false },
                            new ProductImage { ImageUrl = "/images/products/h8.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Urban Heavyweight Charcoal Street Hoodie",
                        Description = "Dark charcoal pullover crafted for durability and cold weather insulation. Features extra-deep hood and ribbed side stretch gussets.",
                        Price = 89.00m,
                        StockQuantity = 34,
                        CategoryId = hoodiesCat.Id,
                        MainImageUrl = "/images/products/h9.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/h9.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/h10.jpg", IsMain = false }
                        }
                    },

                    // --- PANTS PRODUCTS ---
                    new Product
                    {
                        Name = "Tactical Multi-Pocket Relaxed Cargo Pants",
                        Description = "Durable ripstop cotton cargo pants featuring 6 utilitarian bellow pockets, adjustable drawstring ankle cuffs, and articulated knee darting.",
                        Price = 85.00m,
                        StockQuantity = 45,
                        CategoryId = pantsCat.Id,
                        MainImageUrl = "/images/products/cargo p.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-27),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/cargo p.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/p.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Wide-Leg Pleated Baggy Street Trouser",
                        Description = "Modern Korean street style baggy trouser with front pleats, comfortable elasticized waistband with belt loops, and dramatic wide leg drape.",
                        Price = 78.00m,
                        StockQuantity = 50,
                        CategoryId = pantsCat.Id,
                        MainImageUrl = "/images/products/baggy trouser.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-24),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/baggy trouser.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/trouser.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Relaxed Fit Tailored Minimalist Trouser",
                        Description = "Versatile trouser tailored from smooth wrinkle-resistant twill with a relaxed thigh and gentle taper toward the ankle.",
                        Price = 72.00m,
                        StockQuantity = 60,
                        CategoryId = pantsCat.Id,
                        MainImageUrl = "/images/products/trouser.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-20),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/trouser.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/pan.jpeg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Vintage Straight-Leg Utility Denim Pants",
                        Description = "Heavyweight 14oz non-stretch denim with vintage wash highlights, reinforced hammer loop, and classic five-pocket configuration.",
                        Price = 89.50m,
                        StockQuantity = 40,
                        CategoryId = pantsCat.Id,
                        MainImageUrl = "/images/products/p1.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-17),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/p1.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/p2.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Heavyweight Canvas Carpenter Work Pants",
                        Description = "Sturdy duck canvas workwear pants built with double-knee panels, triple-needle stitching, and utility tool pockets.",
                        Price = 92.00m,
                        StockQuantity = 35,
                        CategoryId = pantsCat.Id,
                        MainImageUrl = "/images/products/p2.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-13),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/p2.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/pan.jpeg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Loose Fit Drawstring Chino Pants",
                        Description = "Casual breathable cotton chino featuring an internal drawstring waist, relaxed seat, and clean casual break over sneakers.",
                        Price = 69.00m,
                        StockQuantity = 55,
                        CategoryId = pantsCat.Id,
                        MainImageUrl = "/images/products/p5.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-9),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/p5.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/p6.jpg", IsMain = false },
                            new ProductImage { ImageUrl = "/images/products/p3.jpg", IsMain = false },
                            new ProductImage { ImageUrl = "/images/products/p4.jpg", IsMain = false }
                        }
                    },
                    new Product
                    {
                        Name = "Urban Casual Corduroy Wide-Leg Pants",
                        Description = "Cozy wide-wale corduroy pants offering rich texture, warm autumn/winter comfort, and a relaxed stylish silhouette.",
                        Price = 76.00m,
                        StockQuantity = 48,
                        CategoryId = pantsCat.Id,
                        MainImageUrl = "/images/products/p7.jpg",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        IsActive = true,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/p7.jpg", IsMain = true },
                            new ProductImage { ImageUrl = "/images/products/p8.jpg", IsMain = false }
                        }
                    }
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();

                // 6. Seed Reviews and Comments
                if (demoUser != null)
                {
                    var tshirtItem = products.First(p => p.Name.Contains("AW22"));
                    var cardiganItem = products.First(p => p.Name.Contains("Mohair"));
                    var hoodieItem = products.First(p => p.Name.Contains("French Terry"));
                    var pantsItem = products.First(p => p.Name.Contains("Cargo Pants"));

                    var reviews = new List<Review>
                    {
                        new Review
                        {
                            ProductId = hoodieItem.Id,
                            UserId = demoUser.Id,
                            Rating = 5,
                            Comment = "Unbelievable quality and fleece weight! The double-layered hood holds its shape perfectly. Will definitely order in another color.",
                            CreatedAt = DateTime.UtcNow.AddDays(-8)
                        },
                        new Review
                        {
                            ProductId = cardiganItem.Id,
                            UserId = demoUser.Id,
                            Rating = 5,
                            Comment = "Super soft mohair knit, not scratchy at all. The oversized drape looks great layered over simple white tees.",
                            CreatedAt = DateTime.UtcNow.AddDays(-6)
                        },
                        new Review
                        {
                            ProductId = tshirtItem.Id,
                            UserId = demoUser.Id,
                            Rating = 5,
                            Comment = "The vintage wash and print texture look even better in person. Thick collar that doesn't sag.",
                            CreatedAt = DateTime.UtcNow.AddDays(-4)
                        },
                        new Review
                        {
                            ProductId = pantsItem.Id,
                            UserId = demoUser.Id,
                            Rating = 4,
                            Comment = "The cargo pockets are deep and practical. The ankle drawstrings let you style them tapered or wide.",
                            CreatedAt = DateTime.UtcNow.AddDays(-2)
                        }
                    };

                    context.Reviews.AddRange(reviews);

                    var comments = new List<Comment>
                    {
                        new Comment
                        {
                            ProductId = hoodieItem.Id,
                            UserId = demoUser.Id,
                            Content = "Is this true to size or should I size down for a standard fit?",
                            CreatedAt = DateTime.UtcNow.AddDays(-7)
                        },
                        new Comment
                        {
                            ProductId = hoodieItem.Id,
                            UserId = adminUser!.Id,
                            Content = "It is designed with an oversized streetwear cut. If you prefer a tailored fit, we recommend sizing down one size.",
                            CreatedAt = DateTime.UtcNow.AddDays(-6)
                        }
                    };

                    context.Comments.AddRange(comments);

                    // 7. Seed Orders for Dashboard Chart Visualization
                    var order1 = new Order
                    {
                        UserId = demoUser.Id,
                        OrderDate = DateTime.UtcNow.AddDays(-20),
                        TotalAmount = 163.00m,
                        Status = OrderStatus.Delivered,
                        ShippingAddress = "45 Elm Street, Springfield",
                        PhoneNumber = "+1 555-0192",
                        CreatedAt = DateTime.UtcNow.AddDays(-20),
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem { ProductId = hoodieItem.Id, Quantity = 1, UnitPrice = 78.00m },
                            new OrderItem { ProductId = pantsItem.Id, Quantity = 1, UnitPrice = 85.00m }
                        }
                    };

                    var order2 = new Order
                    {
                        UserId = demoUser.Id,
                        OrderDate = DateTime.UtcNow.AddDays(-10),
                        TotalAmount = 142.99m,
                        Status = OrderStatus.Shipped,
                        ShippingAddress = "45 Elm Street, Springfield",
                        PhoneNumber = "+1 555-0192",
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem { ProductId = tshirtItem.Id, Quantity = 1, UnitPrice = 44.99m },
                            new OrderItem { ProductId = cardiganItem.Id, Quantity = 1, UnitPrice = 98.00m }
                        }
                    };

                    var order3 = new Order
                    {
                        UserId = demoUser.Id,
                        OrderDate = DateTime.UtcNow.AddDays(-2),
                        TotalAmount = 78.00m,
                        Status = OrderStatus.Processing,
                        ShippingAddress = "45 Elm Street, Springfield",
                        PhoneNumber = "+1 555-0192",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem { ProductId = products.First(p => p.Name.Contains("Baggy Street")).Id, Quantity = 1, UnitPrice = 78.00m }
                        }
                    };

                    context.Orders.AddRange(order1, order2, order3);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
