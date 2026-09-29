using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ggChick.Data;
using ggChick.Models;
using ggChick.Services;
using ggChick.ViewModels.Products;

namespace ggChick.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public ProductsController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // GET: Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            var model = new ProductCreateEditViewModel
            {
                CategoriesList = new SelectList(categories, "Id", "Name"),
                IsActive = true
            };
            return View(model);
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? mainImagePath = model.MainImageUrl;

                    // Handle single main image file upload if supplied
                    if (model.MainImageFile != null && model.MainImageFile.Length > 0)
                    {
                        mainImagePath = await _fileService.UploadImageAsync(model.MainImageFile, "products");
                    }

                    var product = new Product
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Price = model.Price,
                        StockQuantity = model.StockQuantity,
                        CategoryId = model.CategoryId,
                        MainImageUrl = mainImagePath ?? "/images/placeholder.png",
                        IsActive = model.IsActive,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // If main image was uploaded, also register in ProductImages table
                    if (!string.IsNullOrEmpty(mainImagePath))
                    {
                        _context.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = mainImagePath,
                            IsMain = true
                        });
                    }

                    // Handle multiple additional images upload
                    if (model.AdditionalImageFiles != null && model.AdditionalImageFiles.Any())
                    {
                        var additionalPaths = await _fileService.UploadMultipleImagesAsync(model.AdditionalImageFiles, "products");
                        foreach (var path in additionalPaths)
                        {
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = path,
                                IsMain = false
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to save product: {ex.Message}");
                }
            }

            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            model.CategoriesList = new SelectList(categories, "Id", "Name", model.CategoryId);
            return View(model);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            var model = new ProductCreateEditViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                IsActive = product.IsActive,
                MainImageUrl = product.MainImageUrl,
                CategoriesList = new SelectList(categories, "Id", "Name", product.CategoryId),
                ExistingImages = product.Images.ToList()
            };

            return View(model);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCreateEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var product = await _context.Products
                        .Include(p => p.Images)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (product == null) return NotFound();

                    // Handle new main image upload if provided
                    if (model.MainImageFile != null && model.MainImageFile.Length > 0)
                    {
                        var newMainPath = await _fileService.UploadImageAsync(model.MainImageFile, "products");
                        if (!string.IsNullOrEmpty(newMainPath))
                        {
                            product.MainImageUrl = newMainPath;
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = newMainPath,
                                IsMain = true
                            });
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(model.MainImageUrl))
                    {
                        product.MainImageUrl = model.MainImageUrl;
                    }

                    // Handle additional images upload
                    if (model.AdditionalImageFiles != null && model.AdditionalImageFiles.Any())
                    {
                        var additionalPaths = await _fileService.UploadMultipleImagesAsync(model.AdditionalImageFiles, "products");
                        foreach (var path in additionalPaths)
                        {
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = path,
                                IsMain = false
                            });
                        }
                    }

                    product.Name = model.Name;
                    product.Description = model.Description;
                    product.Price = model.Price;
                    product.StockQuantity = model.StockQuantity;
                    product.CategoryId = model.CategoryId;
                    product.IsActive = model.IsActive;
                    product.UpdatedAt = DateTime.UtcNow;

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to update product: {ex.Message}");
                }
            }

            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            model.CategoriesList = new SelectList(categories, "Id", "Name", model.CategoryId);
            return View(model);
        }

        // POST: Admin/Products/DeleteImage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int productId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image != null && image.ProductId == productId)
            {
                _fileService.DeleteFile(image.ImageUrl);
                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Image removed.";
            }

            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                // Delete associated uploaded image files
                if (!string.IsNullOrEmpty(product.MainImageUrl) && product.MainImageUrl.StartsWith("/uploads/"))
                {
                    _fileService.DeleteFile(product.MainImageUrl);
                }

                foreach (var img in product.Images)
                {
                    if (img.ImageUrl.StartsWith("/uploads/"))
                    {
                        _fileService.DeleteFile(img.ImageUrl);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
