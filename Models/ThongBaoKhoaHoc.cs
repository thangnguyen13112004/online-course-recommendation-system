using System;
using System.Collections.Generic;

namespace online_course_recommendation_system.Models;

public partial class ThongBaoKhoaHoc
{
    public int MaThongBao { get; set; }

    public int MaKhoaHoc { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public DateTime? NgayTao { get; set; }

    public virtual KhoaHoc MaKhoaHocNavigation { get; set; } = null!;
}
