using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_course_recommendation_system.Data;

namespace online_course_recommendation_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "GiaoVien,Admin")]
    public class InstructorController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InstructorController(AppDbContext context)
        {
            _context = context;
        }

        // ① GET /api/instructor/courses — Khóa học của giảng viên
        [HttpGet("courses")]
        public async Task<IActionResult> GetMyCourses()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token không hợp lệ." });

            var courses = await _context.GiangVienKhoaHocs
                .Where(gv => gv.MaGiangVien == userId.Value)
                .Include(gv => gv.MaKhoaHocNavigation)
                    .ThenInclude(k => k.MaTheLoaiNavigation)
                .Include(gv => gv.MaKhoaHocNavigation)
                    .ThenInclude(k => k.TienDos)
                .Include(gv => gv.MaKhoaHocNavigation)
                    .ThenInclude(k => k.DanhGia)
                .Select(gv => new
                {
                    gv.MaKhoaHocNavigation.MaKhoaHoc,
                    gv.MaKhoaHocNavigation.TieuDe,
                    gv.MaKhoaHocNavigation.TinhTrang,
                    gv.MaKhoaHocNavigation.GiaGoc,
                    gv.MaKhoaHocNavigation.TbdanhGia,
                    gv.MaKhoaHocNavigation.AnhUrl,
                    gv.MaKhoaHocNavigation.NgayTao,
                    TheLoai = gv.MaKhoaHocNavigation.MaTheLoaiNavigation != null
                        ? gv.MaKhoaHocNavigation.MaTheLoaiNavigation.Ten : null,
                    SoHocVien = gv.MaKhoaHocNavigation.TienDos.Count,
                    SoLuongDanhGia = gv.MaKhoaHocNavigation.DanhGia.Count,
                    gv.LaGiangVienChinh
                })
                .ToListAsync();

            return Ok(courses);
        }

        // ② GET /api/instructor/students — Danh sách học viên
        [HttpGet("students")]
        public async Task<IActionResult> GetStudents(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token không hợp lệ." });

            // Lấy tất cả khóa học mà giảng viên dạy
            var courseIds = await _context.GiangVienKhoaHocs
                .Where(gv => gv.MaGiangVien == userId.Value)
                .Select(gv => gv.MaKhoaHoc)
                .ToListAsync();

            var query = _context.TienDos
                .Where(t => t.MaKhoaHoc.HasValue && courseIds.Contains(t.MaKhoaHoc.Value))
                .Include(t => t.MaNguoiDungNavigation)
                .Include(t => t.MaKhoaHocNavigation);

            var totalCount = await query.CountAsync();

            var students = await query
                .OrderByDescending(t => t.NgayThamGia)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    HocVien = t.MaNguoiDungNavigation == null ? null : new
                    {
                        t.MaNguoiDungNavigation.MaNguoiDung,
                        t.MaNguoiDungNavigation.Ten,
                        t.MaNguoiDungNavigation.Email,
                        t.MaNguoiDungNavigation.LinkAnhDaiDien
                    },
                    KhoaHoc = t.MaKhoaHocNavigation == null ? null : new
                    {
                        t.MaKhoaHocNavigation.MaKhoaHoc,
                        t.MaKhoaHocNavigation.TieuDe
                    },
                    t.PhanTramTienDo,
                    t.TinhTrang,
                    t.NgayThamGia
                })
                .ToListAsync();

            return Ok(new { totalCount, page, pageSize, data = students });
        }

        // ③ GET /api/instructor/stats — Thống kê tổng quan
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token không hợp lệ." });

            var courseIds = await _context.GiangVienKhoaHocs
                .Where(gv => gv.MaGiangVien == userId.Value)
                .Select(gv => gv.MaKhoaHoc)
                .ToListAsync();

            var tongKhoaHoc = courseIds.Count;

            var tongHocVien = await _context.TienDos
                .Where(t => t.MaKhoaHoc.HasValue && courseIds.Contains(t.MaKhoaHoc.Value))
                .Select(t => t.MaNguoiDung)
                .Distinct()
                .CountAsync();

            var tbDanhGia = await _context.DanhGia
                .Where(d => d.MaKhoaHoc.HasValue && courseIds.Contains(d.MaKhoaHoc.Value) && d.Rating.HasValue)
                .AverageAsync(d => (double?)d.Rating) ?? 0;

            var tongDoanhThu = await _context.ChiTietHoaDons
                .Where(ct => ct.MaKhoaHoc.HasValue && courseIds.Contains(ct.MaKhoaHoc.Value))
                .SumAsync(ct => ct.Gia ?? 0);

            var tongDanhGia = await _context.DanhGia
                .Where(d => d.MaKhoaHoc.HasValue && courseIds.Contains(d.MaKhoaHoc.Value))
                .CountAsync();

            return Ok(new
            {
                tongKhoaHoc,
                tongHocVien,
                tbDanhGia = Math.Round(tbDanhGia, 1),
                tongDoanhThu,
                tongDanhGia
            });
        }

        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }
}
