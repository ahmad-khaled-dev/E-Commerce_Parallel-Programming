namespace E_Commerce.Application.DTOs.Batch
{
    public class BatchSalesReportDto
    {
        public string Approach { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public int TotalOrdersProcessed { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ChunkSize { get; set; }
        public int TotalChunks { get; set; }
        public long ElapsedMs { get; set; }
        public string Verdict { get; set; } = string.Empty;
    }
}
