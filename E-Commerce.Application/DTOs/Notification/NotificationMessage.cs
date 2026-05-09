namespace E_Commerce.Application.DTOs.Notification
{
    public class NotificationMessage
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Type { get; set; } = "OrderConfirmation"; // e.g. InvoiceGeneration, EmailConfirmation
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
