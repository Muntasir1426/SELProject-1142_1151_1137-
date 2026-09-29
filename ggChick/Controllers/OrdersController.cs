using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ggChick.Data;
using ggChick.Models;
using ggChick.Models.Enums;
using ggChick.Services;
using ggChick.ViewModels.Orders;
using System.Security.Claims;

namespace ggChick.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(
            ApplicationDbContext context,
            ICartService cartService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _cartService = cartService;
            _userManager = userManager;
        }

        // GET: /Orders/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items before checking out.";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            var model = new CheckoutViewModel
            {
                FullName = user?.FullName ?? "",
                Email = user?.Email ?? "",
                ShippingAddress = user?.Address ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
                Cart = cart
            };

            return View(model);
        }

        // POST: /Orders/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = _cartService.GetCart();
            model.Cart = cart;

            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "Your cart is empty.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Verify stock availability
            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError("", $"Sorry, '{item.ProductName}' only has {product?.StockQuantity ?? 0} in stock.");
                    return View(model);
                }
            }

            // Create Order
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = cart.GrandTotal,
                Status = OrderStatus.Pending,
                ShippingAddress = model.ShippingAddress,
                PhoneNumber = model.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };

            // Create Order Items and adjust stock
            foreach (var item in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
                order.OrderItems.Add(orderItem);

                // Deduct stock
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= item.Quantity;
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Clear session cart
            _cartService.ClearCart();

            return RedirectToAction("Confirmation", new { id = order.Id });
        }

        // GET: /Orders/Confirmation/5
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && (o.UserId == userId || User.IsInRole("Admin")));

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: /Orders
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: /Orders/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && (o.UserId == userId || User.IsInRole("Admin")));

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
