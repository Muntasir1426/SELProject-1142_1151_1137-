using Microsoft.AspNetCore.Mvc;
using ggChick.Data;
using ggChick.Services;

namespace ggChick.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ApplicationDbContext _context;

        public CartController(ICartService cartService, ApplicationDbContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            return View(cart);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Product not found or unavailable." });
                }
                TempData["ErrorMessage"] = "Product not found or unavailable.";
                return RedirectToAction("Index", "Products");
            }

            if (product.StockQuantity < 1)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Sorry, this product is currently out of stock." });
                }
                TempData["ErrorMessage"] = "Product is out of stock.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            _cartService.AddToCart(
                product.Id,
                product.Name,
                product.Price,
                product.MainImageUrl,
                product.StockQuantity,
                quantity);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    message = $"Added {product.Name} to cart!",
                    cartCount = _cartService.GetCartCount()
                });
            }

            TempData["SuccessMessage"] = $"Added {product.Name} to cart!";
            return RedirectToAction("Index");
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            _cartService.UpdateQuantity(productId, quantity);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var cart = _cartService.GetCart();
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                return Json(new
                {
                    success = true,
                    itemSubtotal = item?.Subtotal ?? 0,
                    cartSubtotal = cart.Subtotal,
                    estimatedTax = cart.EstimatedTax,
                    shippingFee = cart.ShippingFee,
                    grandTotal = cart.GrandTotal,
                    cartCount = cart.TotalItemCount
                });
            }

            return RedirectToAction("Index");
        }

        // POST: /Cart/Remove
        [HttpPost]
        public IActionResult Remove(int productId)
        {
            _cartService.RemoveFromCart(productId);
            TempData["SuccessMessage"] = "Item removed from your cart.";
            return RedirectToAction("Index");
        }

        // POST: /Cart/Clear
        [HttpPost]
        public IActionResult Clear()
        {
            _cartService.ClearCart();
            TempData["SuccessMessage"] = "Cart cleared.";
            return RedirectToAction("Index");
        }

        // GET: /Cart/GetCount
        [HttpGet]
        public IActionResult GetCount()
        {
            return Json(new { count = _cartService.GetCartCount() });
        }
    }
}
