namespace InventoryZeroAPI.DTOs.Admin
{
    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalShops { get; set; }
        public int PendingShops { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PlatformFees { get; set; }
        public decimal PendingPayouts { get; set; }
        public int DisputesOpen { get; set; }
    }
}