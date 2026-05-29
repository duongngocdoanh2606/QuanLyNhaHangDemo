using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhaHangDemo.Models
{
    public enum TableStatus
    {
        Empty = 0,
        Serving = 1,
        Reserved = 2
    }

    public class TableModel
    {
        public int Id { get; set; }

        public string TableName { get; set; }

        public TableStatus Status { get; set; }
            = TableStatus.Empty;

        public float PosX { get; set; } = 0;

        public float PosY { get; set; } = 0;

        public float Width { get; set; } = 100;

        public float Height { get; set; } = 100;

        public int Capacity { get; set; }

        [ForeignKey("ZoneId")]
        public int ZoneId { get; set; }

        public ZoneModel Zone { get; set; }


        // ORDER HIỆN TẠI CỦA BÀN
        public int? CurrentOrderId { get; set; }

        [ForeignKey("CurrentOrderId")]
        public virtual OrderModel? CurrentOrder { get; set; }
    }

    public class TableLayoutDto
    {
        public int Id { get; set; }

        public float PosX { get; set; }

        public float PosY { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }
    }
}