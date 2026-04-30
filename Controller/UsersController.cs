using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_course_recommendation_system.Data;
using online_course_recommendation_system.DTO;
using System.IO;
using System.Text.Json;

namespace online_course_recommendation_system.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("ping")]
        public IActionResult Ping() => Ok("pong");

        // ⑨ GET /api/users/debugroute — Lấy cấu hình nhận thông báo
        [AllowAnonymous]
        [HttpGet("debugroute")]
        public async Task<IActionResult> GetNotificationSettings()
        {
            var userId = GetUserIdFromToken();
            if (userId == null) return Unauthorized();

            var user = await _context.NguoiDungs.FindAsync(userId.Value);
            if (user == null) return NotFound();

            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "user_notifications.json");
            if (System.IO.File.Exists(settingsPath))
            {
                var json = await System.IO.File.ReadAllTextAsync(settingsPath);
                try {
                    var allSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (allSettings != null && allSettings.TryGetValue(userId.Value.ToString(), out var userSettings))
                    {
                        return Ok(userSettings);
                    }
                } catch { }
            }

            return Ok(GetDefaultSettings(user.VaiTro ?? "HocVien"));
        }

        // ⑩ POST /api/users/debugroute — Cập nhật cấu hình nhận thông báo
        [AllowAnonymous]
        [HttpPost("debugroute")]
        public async Task<IActionResult> UpdateNotificationSettings([FromBody] JsonElement settings)
        {
            var userId = GetUserIdFromToken();
            if (userId == null) return Unauthorized();

            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "user_notifications.json");
            Dictionary<string, JsonElement> allSettings = new();

            if (System.IO.File.Exists(settingsPath))
            {
                var json = await System.IO.File.ReadAllTextAsync(settingsPath);
                try {
                    allSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
                } catch { allSettings = new(); }
            }

            allSettings[userId.Value.ToString()] = settings;

            var dir = Path.GetDirectoryName(settingsPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            var updatedJson = JsonSerializer.Serialize(allSettings, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(settingsPath, updatedJson);

            return Ok(new { message = "Cập nhật cấu hình thông báo thành công!" });
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
        [HttpGet("{id:int}")]
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
            [FromQuery] string? search = null,
            [FromQuery] string? vaiTro = null)
        {
            var query = _context.NguoiDungs.AsQueryable();

            // Tìm kiếm theo tên hoặc email
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Ten.Contains(search) || u.Email.Contains(search));
            }

            // Lọc theo vai trò (HocVien, GiaoVien, Admin)
            if (!string.IsNullOrWhiteSpace(vaiTro))
            {
                query = query.Where(u => u.VaiTro == vaiTro);
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

        [Authorize(Roles = "Admin")]
        [HttpGet("stats")]
        public async Task<IActionResult> GetAdminStats()
        {
            var totalUsers = await _context.NguoiDungs.CountAsync();
            var students = await _context.NguoiDungs.CountAsync(u => u.VaiTro == "HocVien");
            var instructors = await _context.NguoiDungs.CountAsync(u => u.VaiTro == "GiaoVien");
            var admins = await _context.NguoiDungs.CountAsync(u => u.VaiTro == "Admin");

            return Ok(new
            {
                totalUsers,
                students,
                instructors,
                admins
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

        // ⑦ GET /api/users/notifications — Lấy danh sách thông báo của bản thân
        [Authorize]
        [HttpGet("notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = GetUserIdFromToken();
            if (userId == null) return Unauthorized();

            var notifications = await _context.ThongBaos
                .Where(t => t.MaNguoiDung == userId.Value)
                .OrderByDescending(t => t.NgayTao)
                .Take(50) // Giới hạn lấy 50 thông báo gần nhất
                .Select(t => new
                {
                    t.MaThongBao,
                    t.TieuDe,
                    t.NoiDung,
                    t.NgayTao,
                    t.DaDoc
                })
                .ToListAsync();

            var unreadCount = await _context.ThongBaos.CountAsync(t => t.MaNguoiDung == userId.Value && !t.DaDoc);

            return Ok(new { data = notifications, unreadCount });
        }

        // ⑧ PUT /api/users/notifications/{id}/read — Đánh dấu đã đọc
        [Authorize]
        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var userId = GetUserIdFromToken();
            if (userId == null) return Unauthorized();

            var notif = await _context.ThongBaos.FirstOrDefaultAsync(t => t.MaThongBao == id && t.MaNguoiDung == userId.Value);
            if (notif == null) return NotFound(new { message = "Không tìm thấy thông báo." });

            notif.DaDoc = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã đánh dấu đọc." });
        }

        private object GetDefaultSettings(string vaiTro)
        {
            if (vaiTro == "GiaoVien")
            {
                return new[]
                {
                    new { id = "enrollment", label = "Học viên mới đăng ký", enabled = true },
                    new { id = "review", label = "Đánh giá mới", enabled = true },
                    new { id = "revenue", label = "Báo cáo doanh thu", enabled = true },
                    new { id = "approval", label = "Trạng thái khóa học", enabled = true },
                    new { id = "promotion", label = "Khuyến mãi hệ thống", enabled = false }
                };
            }

            return new[]
            {
                new { id = "purchase", label = "Xác nhận mua khóa học", enabled = true },
                new { id = "expiry", label = "Nhắc nhở quá hạn", enabled = true },
                new { id = "update", label = "Cập nhật nội dung", enabled = true },
                new { id = "promotion", label = "Khuyến mãi & Ưu đãi", enabled = false }
            };
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
