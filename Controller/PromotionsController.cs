using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_course_recommendation_system.Data;
using online_course_recommendation_system.DTO;
using online_course_recommendation_system.Models;

namespace online_course_recommendation_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PromotionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PromotionsController(AppDbContext context)
        {
            _context = context;
        }

        // ① GET /api/promotions — Lấy tất cả khuyến mãi
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var promotions = await _context.KhuyenMais
                .Select(km => new
                {
                    km.MaKhuyenMai,
                    km.TenChuongTrinh,
                    km.PhanTramGiam,
                    km.NgayBatDau,
                    km.NgayKetThuc,
                    SoKhoaHoc = km.KhoaHocs.Count,
                    TinhTrang = km.NgayKetThuc != null && km.NgayKetThuc < DateTime.Now
                        ? "expired" : "active"
                })
                .OrderByDescending(km => km.NgayBatDau)
                .ToListAsync();

            return Ok(promotions);
        }

        // ② GET /api/promotions/{id} — Chi tiết 1 khuyến mãi
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var promo = await _context.KhuyenMais
                .Where(km => km.MaKhuyenMai == id)
                .Select(km => new
                {
                    km.MaKhuyenMai,
                    km.TenChuongTrinh,
                    km.PhanTramGiam,
                    km.NgayBatDau,
                    km.NgayKetThuc,
                    SoKhoaHoc = km.KhoaHocs.Count,
                    TinhTrang = km.NgayKetThuc != null && km.NgayKetThuc < DateTime.Now
                        ? "expired" : "active"
                })
                .FirstOrDefaultAsync();

            if (promo == null)
                return NotFound(new { message = "Không tìm thấy khuyến mãi." });

            return Ok(promo);
        }

        // ③ POST /api/promotions — Tạo khuyến mãi mới
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PromotionDto request)
        {
            if (string.IsNullOrWhiteSpace(request.TenChuongTrinh))
                return BadRequest(new { message = "Tên chương trình không được để trống." });

            var promo = new KhuyenMai
            {
                TenChuongTrinh = request.TenChuongTrinh,
                PhanTramGiam = request.PhanTramGiam,
                NgayBatDau = request.NgayBatDau ?? DateTime.Now,
                NgayKetThuc = request.NgayKetThuc
            };

            _context.KhuyenMais.Add(promo);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo khuyến mãi thành công!",
                data = new
                {
                    promo.MaKhuyenMai,
                    promo.TenChuongTrinh,
                    promo.PhanTramGiam,
                    promo.NgayBatDau,
                    promo.NgayKetThuc
                }
            });
        }

        // ④ PUT /api/promotions/{id} — Cập nhật khuyến mãi
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PromotionDto request)
        {
            var promo = await _context.KhuyenMais.FindAsync(id);
            if (promo == null)
                return NotFound(new { message = "Không tìm thấy khuyến mãi." });

            if (!string.IsNullOrWhiteSpace(request.TenChuongTrinh))
                promo.TenChuongTrinh = request.TenChuongTrinh;
            if (request.PhanTramGiam.HasValue)
                promo.PhanTramGiam = request.PhanTramGiam;
            if (request.NgayBatDau.HasValue)
                promo.NgayBatDau = request.NgayBatDau;
            if (request.NgayKetThuc.HasValue)
                promo.NgayKetThuc = request.NgayKetThuc;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật khuyến mãi thành công!",
                data = new
                {
                    promo.MaKhuyenMai,
                    promo.TenChuongTrinh,
                    promo.PhanTramGiam,
                    promo.NgayBatDau,
                    promo.NgayKetThuc
                }
            });
        }

        // ⑤ DELETE /api/promotions/{id} — Xóa khuyến mãi
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var promo = await _context.KhuyenMais
                .Include(km => km.KhoaHocs)
                .FirstOrDefaultAsync(km => km.MaKhuyenMai == id);

            if (promo == null)
                return NotFound(new { message = "Không tìm thấy khuyến mãi." });

            if (promo.KhoaHocs.Any())
                return BadRequest(new { message = $"Không thể xóa. Khuyến mãi '{promo.TenChuongTrinh}' đang gắn với {promo.KhoaHocs.Count} khóa học." });

            _context.KhuyenMais.Remove(promo);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa khuyến mãi '{promo.TenChuongTrinh}'." });
        }
    }
}
