using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Areas.Admin.Repository;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using System.Security.Claims;

namespace QuanLyNhaHangDemo.Controllers
{
    public class ReservationController : Controller
    {
        private readonly DataContext _context;

        private readonly IEmailSender _emailSender;

        public ReservationController(DataContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // GET: /Reservation/Create?tableId=1
        [HttpGet]
        public async Task<IActionResult> Create(int tableId)
        {
            var table = await _context.Table.FindAsync(tableId);
            if (table == null) return NotFound();

            ViewBag.Table = table;

            var model = new ReservationModel
            {
                TableId = tableId,
                ReserveTime = DateTime.Now.AddHours(1)
            };
            return View(model);
        }

        // POST: /Reservation/Create
        // POST: /Reservation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReservationModel model)
        {
            var table = await _context.Table.FindAsync(model.TableId);
            if (table == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Table = table;
                return View(model);
            }

            string? userEmail = User.FindFirstValue(ClaimTypes.Email);
            model.CustomerEmail = userEmail;
            model.Status = ReservationStatus.Pending;
            model.CreatedAt = DateTime.Now;

            _context.Reservations.Add(model);
            await _context.SaveChangesAsync();

            var adminEmail = "doanhdkdk@gmail.com";
            var subject = $"Yêu cầu đặt bàn #{model.Id}";
            var message = $@"
                Bạn có một yêu cầu đặt bàn mới:

                Bàn: {table.TableName} (ID: {table.Id})
                Khách: {model.CustomerName}
                SĐT: {model.Phone}
                Số người: {model.PeopleCount}
                Thời gian đến: {model.ReserveTime:dd/MM/yyyy HH:mm}
                Ghi chú: {model.Note}

                Vui lòng vào trang quản trị để duyệt yêu cầu.";

            await _emailSender.SendEmailAsync(adminEmail, subject, message);

            TempData["success"] = "Gửi yêu cầu đặt bàn thành công. Vui lòng chờ xác nhận.";
            return RedirectToAction("Index", "Home");
        }

    }
}
