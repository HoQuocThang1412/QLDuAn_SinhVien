using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Models;
using System.Diagnostics;

namespace QuanLySinhVien.Controllers
{
    [Authorize] // Bắt buộc phải đăng nhập mới được vào Controller này
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // 🛑 1. KIỂM TRA PHÂN QUYỀN ĐIỀU HƯỚNG

            // Nếu là Sinh Viên -> Đẩy thẳng về trang Tra Cứu Điểm
            if (User.IsInRole("SinhVien"))
            {
                return RedirectToAction("TraCuu", "Diem");
            }

            // Nếu là Giảng Viên -> Đẩy về trang Quản lý Điểm (hàm Index của DiemController sẽ tự chuyển tới Nhập Điểm)
            if (User.IsInRole("GiangVien"))
            {
                return RedirectToAction("Index", "Diem");
            }

            // 🛑 2. NẾU LÀ ADMIN HOẶC CÁN BỘ -> Cho phép ở lại xem giao diện Dashboard thống kê
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Hàm hỗ trợ tạo mật khẩu mã hóa test nhanh (nhớ ẩn/xóa đi khi nộp đồ án nha sếp)
        [AllowAnonymous]
        public IActionResult TaoHash(string matKhau = "123456")
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(matKhau);
            return Content(hash);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}