using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace online_course_recommendation_system.Models
{
    [Table("NguoiDung")]
    public class NguoiDung
    {
        [Key]
        public int MaNguoiDung { get; set; }
        public string Ten { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty; // HocVien, GiaoVien, Admin
        public string? LinkAnhDaiDien { get; set; }
        public string? TieuSu { get; set; }
        public string TinhTrang { get; set; } = "Hoạt động";
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
