using ggChick.Models;

namespace ggChick.ViewModels.Admin
{
    public class DashboardStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalReviews { get; set; }

        public List<Order> RecentOrders { get; set; } = new List<Order>();
    }

    public class ChartDataViewModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Values { get; set; } = new List<decimal>();
    }

    public class DashboardChartsDataViewModel
    {
        public ChartDataViewModel SalesOverTime { get; set; } = new ChartDataViewModel();
        public ChartDataViewModel OrdersByStatus { get; set; } = new ChartDataViewModel();
        public ChartDataViewModel TopProducts { get; set; } = new ChartDataViewModel();
        public ChartDataViewModel ProductsByCategory { get; set; } = new ChartDataViewModel();
    }
}
