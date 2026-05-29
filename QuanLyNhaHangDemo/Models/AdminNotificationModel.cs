namespace QuanLyNhaHangDemo.Models
{
    public class AdminNotificationModel
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public int OrderId { get; set; }
        public int OrderDetailId { get; set; }
        public string Message { get; set; }
        public string ProductName { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual TableModel Table { get; set; }
    }
}
