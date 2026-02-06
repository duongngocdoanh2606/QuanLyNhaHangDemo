namespace QuanLyNhaHangDemo.Models.ViewModels
{
    public class OrderViewModel
    {
        public int Id { get; set; }
        public DateTime CreateAt { get; set; }
        public TimeSpan ElapsedTime => DateTime.Now - CreateAt;
        public string ElapsedText
        {
            get
            {
                var t = ElapsedTime;
                if (t.TotalMinutes < 1) return "Vừa đặt";
                if (t.TotalHours < 1) return $"{t.Minutes} phút";
                return $"{(int)t.TotalHours} giờ {t.Minutes} phút";

            }
        }
    }
}
