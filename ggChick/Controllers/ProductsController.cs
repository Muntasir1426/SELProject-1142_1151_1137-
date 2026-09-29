using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ggChick.Data;
using ggChick.ViewModels.Products;
using System.Security.Claims;

namespace ggChick.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Products
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            int? minRating,
            bool inStockOnly,
            string sort = "newest",
            int page = 1,
            int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 48) pageSize = 12;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .AsQueryable();

            // 1. Search by Name or Description
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{term}%") || 
                                         EF.Functions.ILike(p.Description, $"%{term}%"));
            }

            // 2. Filter by Category
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // 3. Filter by Price Range
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // 4. Filter by Availability
            if (inStockOnly)
            {
                query = query.Where(p => p.StockQuantity > 0);
            }

            // 5. Filter by Minimum Average Rating
            if (minRating.HasValue && minRating.Value > 0)
            {
                query = query.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= minRating.Value);
            }

            // 6. Sort
            query = sort switch
            {
                "priceAsc" => query.OrderBy(p => p.Price),
                "priceDesc" => query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
                "nameAsc" => query.OrderBy(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAt) // "newest" default
            };

            // 7. Pagination
            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            var viewModel = new ProductListViewModel
            {
                Products = products,
                Categories = categories,
                Search = search,
                CategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinRating = minRating,
                InStockOnly = inStockOnly,
                Sort = sort,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        // GET: /Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            // Sort comments & reviews by newest
            product.Reviews = product.Reviews.OrderByDescending(r => r.CreatedAt).ToList();
            product.Comments = product.Comments.OrderByDescending(c => c.CreatedAt).ToList();

            // Related products in same category
            var related = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .Include(p => p.Reviews)
                .Take(4)
                .ToListAsync();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingReview = currentUserId != null
                ? product.Reviews.FirstOrDefault(r => r.UserId == currentUserId)
                : null;

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                RelatedProducts = related,
                UserHasReviewed = existingReview != null,
                UserExistingReview = existingReview,
                NewReview = new ReviewInputModel { ProductId = product.Id, Rating = 5 },
                NewComment = new CommentInputModel { ProductId = product.Id }
            };

            return View(viewModel);
        }

        // JSON Endpoint for Product Search/Filter
        [HttpGet]
        public async Task<IActionResult> GetProducts(string? search, int? categoryId, int limit = 8)
        {
            var query = _context.Products
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{term}%"));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var results = await query
                .Take(limit)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    imageUrl = p.MainImageUrl ?? "/images/placeholder.png",
                    inStock = p.StockQuantity > 0
                })
                .ToListAsync();

            return Json(results);
        }
    }
}
