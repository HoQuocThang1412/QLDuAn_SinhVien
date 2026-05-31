using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Models;

namespace QuanLySinhVien.Controllers
{
    public class BaseController : Controller
    {
        protected readonly QuanLySinhVienContext _db;

        public BaseController(QuanLySinhVienContext db)
        {
            _db = db;
        }

        // Gọi hàm này ở đầu mỗi action cần dropdown KhoaHoc
        protected async Task LoadKhoaHocDropdown()
        {
            ViewBag.DSKhoaHoc = await _db.KhoaHocs
                .OrderBy(k => k.MaKhoaHoc)
                .Select(k => k.MaKhoaHoc)
                .ToListAsync();
        }
    }
}