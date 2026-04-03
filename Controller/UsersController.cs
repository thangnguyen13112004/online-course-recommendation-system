using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_course_recommendation_system.Data;
using online_course_recommendation_system.DTO;

namespace online_course_recommendation_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // ① GET /api/users/profile — Xem profile bản thân (cần đăng nhập)
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token không hợp lệ." });

            var user = await _context.NguoiDungs.FindAsync(userId.Value);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            return Ok(MapToProfileDto(user));
        }

        // ② PUT /api/users/profile — Cập nhật profile bản thân (cần đăng nhập)
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto request)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token không hợp lệ." });

            var user = await _context.NguoiDungs.FindAsync(userId.Value);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            // Chỉ cập nhật những field được gửi lên (không null)
            if (request.Ten != null) user.Ten = request.Ten;
            if (request.TieuSu != null) user.TieuSu = request.TieuSu;
            if (request.LinkAnhDaiDien != null) user.LinkAnhDaiDien = request.LinkAnhDaiDien;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật profile thành công!", data = MapToProfileDto(user) });
        }

        // ②.5 DELETE /api/users/profile — Vô hiệu hóa tài khoản thân (chuyển sang "Bị khóa")
        [Authorize]
        [HttpDelete("profile")]
        public async Task<IActionResult> DeactivateMyProfile()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Token không hợp lệ." });

            var user = await _context.NguoiDungs.FindAsync(userId.Value);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            user.TinhTrang = "Bị khóa"; // Tương đương vô hiệu hóa
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tài khoản của bạn đã bị ngừng hoạt động." });
        }

        // ③ GET /api/users/{id} — Xem profile công khai (không cần đăng nhập)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPublicProfile(int id)
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            // Trả về thông tin công khai — ẩn Email, TinhTrang
            return Ok(new
            {
                user.MaNguoiDung,
                user.Ten,
                user.VaiTro,
                user.LinkAnhDaiDien,
                user.TieuSu,
                user.NgayTao
            });
        }

        // ④ GET /api/users — Admin lấy danh sách users (phân trang + tìm kiếm)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var query = _context.NguoiDungs.AsQueryable();

            // Tìm kiếm theo tên hoặc email
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Ten.Contains(search) || u.Email.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserProfileDto
                {
                    MaNguoiDung = u.MaNguoiDung,
                    Ten = u.Ten,
                    Email = u.Email,
                    VaiTro = u.VaiTro,
                    LinkAnhDaiDien = u.LinkAnhDaiDien,
                    TieuSu = u.TieuSu,
                    TinhTrang = u.TinhTrang,
                    NgayTao = u.NgayTao
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                data = users
            });
        }

        // ⑤ PUT /api/users/{id}/role — Admin thay đổi vai trò
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto request)
        {
            try
            {
                // Validate vai trò hợp lệ
                var validRoles = new[] { "HocVien", "GiaoVien", "Admin" };
                if (!validRoles.Contains(request.VaiTro))
                    return BadRequest(new { message = $"Vai trò không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validRoles)}" });

                // Không cho Admin tự hạ vai trò chính mình
                var currentUserId = GetUserIdFromToken();
                if (currentUserId == id)
                    return BadRequest(new { message = "Không thể thay đổi vai trò của chính mình." });

                var user = await _context.NguoiDungs.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng." });

                user.VaiTro = request.VaiTro;
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Đã cập nhật vai trò của '{user.Ten}' thành '{request.VaiTro}'." });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Lỗi server: {innerMsg}" });
            }
        }

        // ⑥ PUT /api/users/{id}/status — Admin khóa/mở khóa tài khoản
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto request)
        {
            // Validate trạng thái hợp lệ
            var validStatuses = new[] { "Hoạt động", "Bị khóa" };
            if (!validStatuses.Contains(request.TinhTrang))
                return BadRequest(new { message = $"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}" });

            // Không cho Admin tự khóa chính mình
            var currentUserId = GetUserIdFromToken();
            if (currentUserId == id)
                return BadRequest(new { message = "Không thể thay đổi trạng thái của chính mình." });

            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            user.TinhTrang = request.TinhTrang;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã cập nhật trạng thái của '{user.Ten}' thành '{request.TinhTrang}'." });
        }

        // ==========================================
        // HÀM HỖ TRỢ
        // ==========================================

        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }

        private static UserProfileDto MapToProfileDto(Models.NguoiDung user)
        {
            return new UserProfileDto
            {
                MaNguoiDung = user.MaNguoiDung,
                Ten = user.Ten,
                Email = user.Email,
                VaiTro = user.VaiTro,
                LinkAnhDaiDien = user.LinkAnhDaiDien,
                TieuSu = user.TieuSu,
                TinhTrang = user.TinhTrang,
                NgayTao = user.NgayTao
            };
        }
    }
}
