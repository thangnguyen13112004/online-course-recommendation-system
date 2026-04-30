using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_course_recommendation_system.Data;

namespace online_course_recommendation_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        // ① GET /api/courses — Danh sách khóa học (phân trang, search, filter)
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? level = null,
            [FromQuery] string? sortBy = null)
        {
            var query = _context.KhoaHocs
                .Include(k => k.MaTheLoaiNavigation)
                .Include(k => k.GiangVienKhoaHocs).ThenInclude(g => g.MaGiangVienNavigation)
                .Include(k => k.MaKhuyenMaiNavigation)
                .Where(k => k.TinhTrang == "Published" && !k.IsDeleted)
                .AsQueryable();

            // Tìm kiếm theo tiều đề
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(k => k.TieuDe.Contains(search) || (k.MoTa != null && k.MoTa.Contains(search)));
            }

            // Filter theo thể loại
            if (categoryId.HasValue)
            {
                query = query.Where(k => k.MaTheLoai == categoryId.Value);
            }

            var totalCount = await query.CountAsync();

            // Sắp xếp
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(k => k.GiaGoc),
                "price_desc" => query.OrderByDescending(k => k.GiaGoc),
                "rating" => query.OrderByDescending(k => k.TbdanhGia),
                "newest" => query.OrderByDescending(k => k.NgayTao),
                "revenue" => query.OrderByDescending(k => k.ChiTietHoaDons.Sum(c => c.Gia ?? 0)),
                _ => query.OrderByDescending(k => k.NgayTao)
            };

            var courses = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(k => new
                {
                    k.MaKhoaHoc,
                    k.TieuDe,
                    k.TieuDePhu,
                    k.MoTa,
                    k.GiaGoc,
                    k.TbdanhGia,
                    k.AnhUrl,
                    k.TinhTrang,
                    k.KiNang,
                    k.ThoiGianHocDuKien,
                    k.ThoiGianChoPhepTre,
                    k.NgayTao,
                    k.NgayCapNhat,
                    TheLoai = k.MaTheLoaiNavigation == null ? null : new
                    {
                        k.MaTheLoaiNavigation.MaTheLoai,
                        k.MaTheLoaiNavigation.Ten
                    },
                    GiangVien = k.GiangVienKhoaHocs.Select(gv => new
                    {
                        gv.MaGiangVienNavigation.MaNguoiDung,
                        gv.MaGiangVienNavigation.Ten,
                        gv.LaGiangVienChinh
                    }),
                    SoLuongDanhGia = k.DanhGia.Count,
                    SoLuongChuong = k.Chuongs.Count,
                    SoHocVien = k.TienDos.Count,
                    KhuyenMai = k.MaKhuyenMaiNavigation == null ? null : new
                    {
                        k.MaKhuyenMaiNavigation.PhanTramGiam,
                        k.MaKhuyenMaiNavigation.NgayKetThuc
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                data = courses
            });
        }

        // ② GET /api/courses/{id} — Chi tiết khóa học
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _context.KhoaHocs
                .Include(k => k.MaTheLoaiNavigation)
                .Include(k => k.GiangVienKhoaHocs).ThenInclude(g => g.MaGiangVienNavigation)
                .Include(k => k.Chuongs).ThenInclude(c => c.BaiHocs)
                .Include(k => k.DanhGia).ThenInclude(d => d.MaNguoiDungNavigation)
                .Include(k => k.MaKhuyenMaiNavigation)
                .Where(k => k.MaKhoaHoc == id && !k.IsDeleted)
                .Select(k => new
                {
                    k.MaKhoaHoc,
                    k.TieuDe,
                    k.TieuDePhu,
                    k.MoTa,
                    k.GiaGoc,
                    k.TbdanhGia,
                    k.AnhUrl,
                    k.TinhTrang,
                    k.KiNang,
                    k.ThoiGianHocDuKien,
                    k.ThoiGianChoPhepTre,
                    k.NgayTao,
                    k.NgayCapNhat,
                    TheLoai = k.MaTheLoaiNavigation == null ? null : new
                    {
                        k.MaTheLoaiNavigation.MaTheLoai,
                        k.MaTheLoaiNavigation.Ten
                    },
                    GiangVien = k.GiangVienKhoaHocs.Select(gv => new
                    {
                        gv.MaGiangVienNavigation.MaNguoiDung,
                        gv.MaGiangVienNavigation.Ten,
                        gv.MaGiangVienNavigation.LinkAnhDaiDien,
                        gv.MaGiangVienNavigation.TieuSu,
                        gv.LaGiangVienChinh
                    }),
                    Chuongs = k.Chuongs.Select(c => new
                    {
                        c.MaChuong,
                        c.TieuDe,
                        BaiHocs = c.BaiHocs.Select(b => new
                        {
                            b.MaBaiHoc,
                            b.LyThuyet,
                            b.LinkVideo,
                            b.BaiTap
                        })
                    }),
                    DanhGia = k.DanhGia.Select(d => new
                    {
                        d.MaDanhGia,
                        d.Rating,
                        d.BinhLuan,
                        d.NgayDanhGia,
                        d.Thich,
                        NguoiDanhGia = d.MaNguoiDungNavigation == null ? null : new
                        {
                            d.MaNguoiDungNavigation.MaNguoiDung,
                            d.MaNguoiDungNavigation.Ten,
                            d.MaNguoiDungNavigation.LinkAnhDaiDien
                        }
                    }),
                    SoLuongDanhGia = k.DanhGia.Count,
                    SoHocVien = k.TienDos.Count,
                    SoLuongChuong = k.Chuongs.Count,
                    SoLuongBaiHoc = k.Chuongs.SelectMany(c => c.BaiHocs).Count(),
                    KhuyenMai = k.MaKhuyenMaiNavigation == null ? null : new
                    {
                        k.MaKhuyenMaiNavigation.PhanTramGiam,
                        k.MaKhuyenMaiNavigation.NgayKetThuc
                    }
                })
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound(new { message = "Không tìm thấy khóa học." });

            return Ok(course);
        }

        // ③ GET /api/courses/{id}/chapters — Danh sách chương & bài học
        [HttpGet("{id}/chapters")]
        public async Task<IActionResult> GetChapters(int id)
        {
            var exists = await _context.KhoaHocs.AnyAsync(k => k.MaKhoaHoc == id);
            if (!exists)
                return NotFound(new { message = "Không tìm thấy khóa học." });

            var chapters = await _context.Chuongs
                .Where(c => c.MaKhoaHoc == id)
                .Select(c => new
                {
                    c.MaChuong,
                    c.TieuDe,
                    BaiHocs = c.BaiHocs.Select(b => new
                    {
                        b.MaBaiHoc,
                        b.LyThuyet,
                        b.LinkVideo,
                        b.BaiTap
                    })
                })
                .ToListAsync();

            return Ok(chapters);
        }

        // ④ GET /api/courses/{id}/reviews — Danh sách đánh giá
        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetReviews(int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var exists = await _context.KhoaHocs.AnyAsync(k => k.MaKhoaHoc == id);
            if (!exists)
                return NotFound(new { message = "Không tìm thấy khóa học." });

            var query = _context.DanhGia
                .Where(d => d.MaKhoaHoc == id)
                .Include(d => d.MaNguoiDungNavigation);

            var totalCount = await query.CountAsync();

            var reviews = await query
                .OrderByDescending(d => d.NgayDanhGia)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    d.MaDanhGia,
                    d.Rating,
                    d.BinhLuan,
                    d.NgayDanhGia,
                    d.Thich,
                    NguoiDanhGia = d.MaNguoiDungNavigation == null ? null : new
                    {
                        d.MaNguoiDungNavigation.MaNguoiDung,
                        d.MaNguoiDungNavigation.Ten,
                        d.MaNguoiDungNavigation.LinkAnhDaiDien
                    }
                })
                .ToListAsync();

            return Ok(new { totalCount, page, pageSize, data = reviews });
        }

        // ⑤ GET /api/courses/admin/all — Admin lấy tất cả khóa học (mọi trạng thái)
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllForAdmin(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? sortBy = null)
        {
            var query = _context.KhoaHocs
                .Include(k => k.MaTheLoaiNavigation)
                .Include(k => k.GiangVienKhoaHocs).ThenInclude(g => g.MaGiangVienNavigation)
                .Include(k => k.MaKhuyenMaiNavigation)
                .Include(k => k.ChiTietHoaDons)
                .Where(k => !k.IsDeleted)
                .AsQueryable();

            // Filter theo trạng thái
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(k => k.TinhTrang == status);
            }
            else
            {
                // Mặc định tab Tất cả không lấy các khóa học Nháp
                query = query.Where(k => k.TinhTrang != "Draft");
            }

            // Tìm kiếm theo tiêu đề
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(k => k.TieuDe.Contains(search));
            }

            var totalCount = await query.CountAsync();

            query = sortBy switch
            {
                "price_asc" => query.OrderBy(k => k.GiaGoc),
                "price_desc" => query.OrderByDescending(k => k.GiaGoc),
                "rating" => query.OrderByDescending(k => k.TbdanhGia),
                "revenue" => query.OrderByDescending(k => k.ChiTietHoaDons.Sum(c => c.Gia ?? 0)),
                "newest" => query.OrderByDescending(k => k.NgayTao),
                _ => query.OrderByDescending(k => k.NgayTao)
            };

            var courses = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(k => new
                {
                    k.MaKhoaHoc,
                    k.TieuDe,
                    k.MoTa,
                    k.GiaGoc,
                    k.TbdanhGia,
                    k.AnhUrl,
                    k.TinhTrang,
                    k.ThoiGianChoPhepTre,
                    k.NgayTao,
                    TheLoai = k.MaTheLoaiNavigation == null ? null : new
                    {
                        k.MaTheLoaiNavigation.MaTheLoai,
                        k.MaTheLoaiNavigation.Ten
                    },
                    GiangVien = k.GiangVienKhoaHocs.Select(gv => new
                    {
                        gv.MaGiangVienNavigation.MaNguoiDung,
                        gv.MaGiangVienNavigation.Ten,
                        gv.LaGiangVienChinh
                    }),
                    SoLuongChuong = k.Chuongs.Count,
                    SoHocVien = k.TienDos.Count,
                    DoanhThu = k.ChiTietHoaDons.Sum(c => c.Gia ?? 0),
                    AdminRevenue = k.ChiTietHoaDons.Sum(c => c.Gia ?? 0) * 0.3m
                })
                .ToListAsync();

            return Ok(new { totalCount, page, pageSize, data = courses });
        }

        // ⑥ PUT /api/courses/{id}/status — Admin duyệt/từ chối khóa học
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] CourseStatusRequest request)
        {
            var validStatuses = new[] { "Published", "Draft", "Rejected", "Pending" };
            if (!validStatuses.Contains(request.TinhTrang))
                return BadRequest(new { message = $"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}" });

            var course = await _context.KhoaHocs.FindAsync(id);
            if (course == null)
                return NotFound(new { message = "Không tìm thấy khóa học." });

            course.TinhTrang = request.TinhTrang;
            if (request.ThoiGianChoPhepTre.HasValue)
            {
                course.ThoiGianChoPhepTre = request.ThoiGianChoPhepTre.Value;
            }
            course.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã cập nhật trạng thái khóa học '{course.TieuDe}' thành '{request.TinhTrang}'." });
        }

        // ⑦ GET /api/courses/{id}/announcements — Lấy danh sách thông báo (Public/Student)
        [HttpGet("{id}/announcements")]
        public async Task<IActionResult> GetAnnouncements(int id)
        {
            var courseExists = await _context.KhoaHocs.AnyAsync(k => k.MaKhoaHoc == id);
            if (!courseExists) return NotFound("Khóa học không tồn tại.");

            var announcements = await _context.ThongBaoKhoaHocs
                .Where(t => t.MaKhoaHoc == id)
                .OrderByDescending(t => t.NgayTao)
                .Select(t => new {
                    t.MaThongBao,
                    t.TieuDe,
                    t.NoiDung,
                    t.NgayTao
                })
                .ToListAsync();

            return Ok(announcements);
        }
    }

    public class CourseStatusRequest
    {
        public string TinhTrang { get; set; } = string.Empty;
        public int? ThoiGianChoPhepTre { get; set; }
    }
}
