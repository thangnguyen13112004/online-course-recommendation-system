using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_course_recommendation_system.Data;
using online_course_recommendation_system.Models;

namespace online_course_recommendation_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InteractionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InteractionsController(AppDbContext context)
        {
            _context = context;
        }

        // ① POST /api/interactions/rate — Đánh giá khóa học
        [HttpPost("rate")]
        public async Task<IActionResult> RateCourse([FromBody] RateRequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token không hợp lệ." });

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { message = "Rating phải từ 1 đến 5." });

            // Kiểm tra đã đánh giá chưa
            var existing = await _context.DanhGia
                .FirstOrDefaultAsync(d => d.MaNguoiDung == userId.Value && d.MaKhoaHoc == request.MaKhoaHoc);

            if (existing != null)
            {
                existing.Rating = request.Rating;
                existing.BinhLuan = request.BinhLuan;
                existing.NgayDanhGia = DateTime.Now;
            }
            else
            {
                _context.DanhGia.Add(new DanhGium
                {
                    MaNguoiDung = userId.Value,
                    MaKhoaHoc = request.MaKhoaHoc,
                    Rating = request.Rating,
                    BinhLuan = request.BinhLuan,
                    NgayDanhGia = DateTime.Now,
                    Thich = 0
                });
            }

            await _context.SaveChangesAsync();

            // Cập nhật trung bình đánh giá khóa học
            var avg = await _context.DanhGia
                .Where(d => d.MaKhoaHoc == request.MaKhoaHoc && d.Rating.HasValue)
                .AverageAsync(d => (double?)d.Rating) ?? 0;

            var course = await _context.KhoaHocs.FindAsync(request.MaKhoaHoc);
            if (course != null)
            {
                course.TbdanhGia = Math.Round(avg, 1);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = existing != null ? "Cập nhật đánh giá thành công!" : "Đánh giá thành công!" });
        }

        // ② POST /api/interactions/like/{courseId} — Like/Unlike khóa học
        [HttpPost("like/{courseId}")]
        public async Task<IActionResult> ToggleLike(int courseId)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token không hợp lệ." });

            var existing = await _context.LuotThichKhoaHocs
                .FirstOrDefaultAsync(l => l.MaNguoiDung == userId.Value && l.MaKhoaHoc == courseId);

            if (existing != null)
            {
                _context.LuotThichKhoaHocs.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã bỏ thích.", liked = false });
            }
            else
            {
                _context.LuotThichKhoaHocs.Add(new LuotThichKhoaHoc
                {
                    MaNguoiDung = userId.Value,
                    MaKhoaHoc = courseId,
                    NgayTao = DateTime.Now
                });
                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã thích!", liked = true });
            }
        }

        // ③ GET /api/interactions/likes — Danh sách khóa học đã like
        [HttpGet("likes")]
        public async Task<IActionResult> GetLikedCourses()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token không hợp lệ." });

            var likes = await _context.LuotThichKhoaHocs
                .Where(l => l.MaNguoiDung == userId.Value)
                .Include(l => l.MaKhoaHocNavigation)
                .Select(l => new
                {
                    l.MaKhoaHocNavigation.MaKhoaHoc,
                    l.MaKhoaHocNavigation.TieuDe,
                    l.MaKhoaHocNavigation.AnhUrl,
                    l.MaKhoaHocNavigation.GiaGoc,
                    l.MaKhoaHocNavigation.TbdanhGia,
                    l.NgayTao
                })
                .ToListAsync();

            return Ok(likes);
        }

        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }

    public class RateRequest
    {
        public int MaKhoaHoc { get; set; }
        public double Rating { get; set; }
        public string? BinhLuan { get; set; }
    }
}
