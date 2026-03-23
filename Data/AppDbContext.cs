using Microsoft.EntityFrameworkCore;
using online_course_recommendation_system.Models;

namespace online_course_recommendation_system.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<NguoiDung> NguoiDungs { get; set; }
    }
}