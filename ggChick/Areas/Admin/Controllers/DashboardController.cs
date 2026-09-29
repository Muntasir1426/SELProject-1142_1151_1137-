using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ggChick.Data;
using ggChick.Models;
using ggChick.Models.Enums;
using ggChick.ViewModels.Admin;

namespace ggChick.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var totalReviews = await _context.Reviews.CountAsync();

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            var model = new DashboardStatsViewModel
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalReviews = totalReviews,
                RecentOrders = recentOrders
            };

            return View(model);
        }

        // GET: /Admin/Dashboard/GetChartData
        [HttpGet]
        public async Task<IActionResult> GetChartData()
        {
            // 1. Sales over time (last 6 months)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
            var startOfMonth = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth && o.Status != OrderStatus.Cancelled)
                .ToListAsync();

            var monthlySales = new ChartDataViewModel();
            for (int i = 0; i < 6; i++)
            {
                var monthDate = startOfMonth.AddMonths(i);
                var label = monthDate.ToString("MMM yyyy");
                var sum = orders
                    .Where(o => o.OrderDate.Year == monthDate.Year && o.OrderDate.Month == monthDate.Month)
                    .Sum(o => o.TotalAmount);

                monthlySales.Labels.Add(label);
                monthlySales.Values.Add(sum);
            }

            // 2. Orders by Status
            var orderStatusGroups = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = (decimal)g.Count() })
                .ToListAsync();

            var ordersByStatus = new ChartDataViewModel();
            foreach (var status in Enum.GetNames(typeof(OrderStatus)))
            {
                var found = orderStatusGroups.FirstOrDefault(g => g.Status == status);
                ordersByStatus.Labels.Add(status);
                ordersByStatus.Values.Add(found?.Count ?? 0);
            }

            // 3. Top-selling products
            var topProductsRaw = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Product != null)
                .GroupBy(oi => oi.Product!.Name)
                .Select(g => new { Name = g.Key, Sold = (decimal)g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Sold)
                .Take(5)
                .ToListAsync();

            var topProducts = new ChartDataViewModel();
            foreach (var p in topProductsRaw)
            {
                topProducts.Labels.Add(p.Name.Length > 20 ? p.Name.Substring(0, 18) + "..." : p.Name);
                topProducts.Values.Add(p.Sold);
            }

            // Fallback for top products if no orders yet
            if (!topProducts.Labels.Any())
            {
                var sampleProducts = await _context.Products.Take(4).ToListAsync();
                foreach (var p in sampleProducts)
                {
                    topProducts.Labels.Add(p.Name.Length > 20 ? p.Name.Substring(0, 18) + "..." : p.Name);
                    topProducts.Values.Add(0);
                }
            }

            // 4. Products by Category
            var categoryCounts = await _context.Categories
                .Select(c => new { c.Name, Count = (decimal)c.Products.Count })
                .ToListAsync();

            var productsByCategory = new ChartDataViewModel();
            foreach (var c in categoryCounts)
            {
                productsByCategory.Labels.Add(c.Name);
                productsByCategory.Values.Add(c.Count);
            }

            return Json(new DashboardChartsDataViewModel
            {
                SalesOverTime = monthlySales,
                OrdersByStatus = ordersByStatus,
                TopProducts = topProducts,
                ProductsByCategory = productsByCategory
            });
        }
    }
}
