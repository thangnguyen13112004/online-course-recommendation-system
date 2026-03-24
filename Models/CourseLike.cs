using System;
using System.Collections.Generic;

namespace online_course_recommendation_system.Models;

public partial class CourseLike
{
    public int LikeId { get; set; }

    public int UserId { get; set; }

    public int CourseId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual KhoaHoc Course { get; set; } = null!;

    public virtual NguoiDung User { get; set; } = null!;
}
