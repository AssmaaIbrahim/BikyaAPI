namespace Bikya.DTOs.DashboardDTOs
{
    public class DashboardStatsDTO
    {
        public decimal TotalSales { get; set; }
        public decimal TotalPlatformProfit { get; set; }
        public decimal TotalSellerProfit { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
