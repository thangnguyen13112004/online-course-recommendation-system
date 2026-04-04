using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using online_course_recommendation_system.Models;

namespace online_course_recommendation_system.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BaiHoc> BaiHocs { get; set; }

    public virtual DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; }

    public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    public virtual DbSet<ChungChi> ChungChis { get; set; }

    public virtual DbSet<Chuong> Chuongs { get; set; }

    public virtual DbSet<CourseLike> CourseLikes { get; set; }

    public virtual DbSet<DanhGium> DanhGia { get; set; }

    public virtual DbSet<GiangVienKhoaHoc> GiangVienKhoaHocs { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhoaHoc> KhoaHocs { get; set; }

    public virtual DbSet<KhuyenMai> KhuyenMais { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<TheLoai> TheLoais { get; set; }

    public virtual DbSet<TienDo> TienDos { get; set; }

    public virtual DbSet<TienDoBaiHoc> TienDoBaiHocs { get; set; }

    public virtual DbSet<VwKhoaHocGiaThucTe> VwKhoaHocGiaThucTes { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaiHoc>(entity =>
        {
            entity.HasKey(e => e.MaBaiHoc).HasName("PK__BaiHoc__3F6433C2C6F4D66D");

            entity.ToTable("BaiHoc");

            entity.Property(e => e.LinkVideo).IsUnicode(false);

            entity.HasOne(d => d.MaChuongNavigation).WithMany(p => p.BaiHocs)
                .HasForeignKey(d => d.MaChuong)
                .HasConstraintName("FK__BaiHoc__MaChuong__73BA3083");
        });

        modelBuilder.Entity<ChiTietGioHang>(entity =>
        {
            entity.HasKey(e => e.MaChiTietGioHang).HasName("PK__ChiTietG__BBF474986810DFED");

            entity.ToTable("ChiTietGioHang", tb => tb.HasTrigger("trg_ChanMuaLaiKhoaHoc"));

            entity.HasIndex(e => new { e.MaGioHang, e.MaKhoaHoc }, "UQ_GioHang_KhoaHoc").IsUnique();

            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MaGioHangNavigation).WithMany(p => p.ChiTietGioHangs)
                .HasForeignKey(d => d.MaGioHang)
                .HasConstraintName("FK__ChiTietGi__MaGio__5812160E");

            entity.HasOne(d => d.MaKhoaHocNavigation).WithMany(p => p.ChiTietGioHangs)
                .HasForeignKey(d => d.MaKhoaHoc)
                .HasConstraintName("FK__ChiTietGi__MaKho__571DF1D5");
        });

        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.HasKey(e => e.MaChiTietHoaDon).HasName("PK__ChiTietH__CFF2C4263C85E06D");

            entity.ToTable("ChiTietHoaDon", tb => tb.HasTrigger("trg_CapNhatTongTienHoaDon"));

            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.MaHoaDon)
                .HasConstraintName("FK__ChiTietHo__MaHoa__5AEE82B9");

            entity.HasOne(d => d.MaKhoaHocNavigation).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.MaKhoaHoc)
                .HasConstraintName("FK__ChiTietHo__MaKho__5BE2A6F2");
        });

        modelBuilder.Entity<ChungChi>(entity =>
        {
            entity.HasKey(e => e.MaChungChi).HasName("PK__ChungChi__BD2C8F39BE1A45B2");

            entity.ToTable("ChungChi");

            entity.Property(e => e.NgayPhat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaKhoaHocNavigation).WithMany(p => p.ChungChis)
                .HasForeignKey(d => d.MaKhoaHoc)
                .HasConstraintName("FK__ChungChi__MaKhoa__66603565");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.ChungChis)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK__ChungChi__MaNguo__6754599E");
        });

        modelBuilder.Entity<Chuong>(entity =>
        {
            entity.HasKey(e => e.MaChuong).HasName("PK__Chuong__0D6A804CADFF10C6");

            entity.ToTable("Chuong");

            entity.Property(e => e.TieuDe).HasMaxLength(255);

            entity.HasOne(d => d.MaKhoaHocNavigation).WithMany(p => p.Chuongs)
                .HasForeignKey(d => d.MaKhoaHoc)
                .HasConstraintName("FK__Chuong__MaKhoaHo__70DDC3D8");
        });

        modelBuilder.Entity<CourseLike>(entity =>
        {
            entity.HasKey(e => e.LikeId).HasName("PK__Course_L__992C79302401A6E6");

            entity.ToTable("Course_Likes");

            entity.HasIndex(e => new { e.UserId, e.CourseId }, "UQ_CourseLike").IsUnique();

            entity.Property(e => e.LikeId).HasColumnName("like_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseLikes)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Course_Li__cours__6442E2C9");

            entity.HasOne(d => d.User).WithMany(p => p.CourseLikes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Course_Li__user___634EBE90");
        });

        modelBuilder.Entity<DanhGium>(entity =>
        {
            entity.HasKey(e => e.MaDanhGia).HasName("PK__DanhGia__AA9515BF4213C3E0");

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("trg_GioiHanDanhGia");
                    tb.HasTrigger("trg_KiemTraDieuKienDanhGia");
                });

            entity.Property(e => e.NgayDanhGia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Thich).HasDefaultValue(0);

            entity.HasOne(d => d.MaKhoaHocNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.MaKhoaHoc)
                .HasConstraintName("FK__DanhGia__MaKhoaH__619B8048");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK__DanhGia__MaNguoi__628FA481");
        });

        modelBuilder.Entity<GiangVienKhoaHoc>(entity =>
        {
            entity.HasKey(e => new { e.MaKhoaHoc, e.MaGiangVien }).HasName("PK__GiangVie__F4F341734D2666E9");

            entity.ToTable("GiangVien_KhoaHoc");

            entity.Property(e => e.LaGiangVienChinh).HasDefaultValue(false);
            entity.Property(e => e.TyLeDoanhThu).HasDefaultValue(0.0);

            entity.HasOne(d => d.MaGiangVienNavigation).WithMany(p => p.GiangVienKhoaHocs)
                .HasForeignKey(d => d.MaGiangVien)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GiangVien__MaGia__5070F446");

            entity.HasOne(d => d.MaKhoaHocNavigation).WithMany(p => p.GiangVienKhoaHocs)
                .HasForeignKey(d => d.MaKhoaHoc)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GiangVien__MaKho__4F7CD00D");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => e.MaGioHang).HasName("PK__GioHang__F5001DA317966621");

            entity.ToTable("GioHang");

            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK__GioHang__MaNguoi__412EB0B6");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaHoaDon).HasName("PK__HoaDon__835ED13BCE52D6A0");

            entity.ToTable("HoaDon");

            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhuongThucThanhToan).HasMaxLength(100);
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK__HoaDon__MaNguoiD__44FF419A");
        });

        modelBuilder.Entity<KhoaHoc>(entity =>
        {
            entity.HasKey(e => e.MaKhoaHoc).HasName("PK__KhoaHoc__48F0FF98A6D29ADE");

            entity.ToTable("KhoaHoc", tb => tb.HasTrigger("trg_ChanPublishKhoaHoc"));

            entity.Property(e => e.AnhUrl)
                .IsUnicode(false)
                .HasColumnName("AnhURL");
            entity.Property(e => e.GiaGoc).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.KiNang).HasMaxLength(255);
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TbdanhGia)
                .HasDefaultValue(0.0)
                .HasColumnName("TBDanhGia");
            entity.Property(e => e.TieuDe).HasMaxLength(255);
            entity.Property(e => e.TieuDePhu).HasMaxLength(255);
            entity.Property(e => e.TinhTrang).HasMaxLength(50);

            entity.HasOne(d => d.MaKhuyenMaiNavigation).WithMany(p => p.KhoaHocs)
                .HasForeignKey(d => d.MaKhuyenMai)
                .HasConstraintName("FK__KhoaHoc__MaKhuye__4CA06362");

            entity.HasOne(d => d.MaTheLoaiNavigation).WithMany(p => p.KhoaHocs)
                .HasForeignKey(d => d.MaTheLoai)
                .HasConstraintName("FK__KhoaHoc__MaTheLo__4BAC3F29");
        });

        modelBuilder.Entity<KhuyenMai>(entity =>
        {
            entity.HasKey(e => e.MaKhuyenMai).HasName("PK__KhuyenMa__6F56B3BD346CB793");

            entity.ToTable("KhuyenMai");

            entity.Property(e => e.NgayBatDau).HasColumnType("datetime");
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime");
            entity.Property(e => e.TenChuongTrinh).HasMaxLength(200);
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__NguoiDun__C539D762B5543294");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.Email, "UQ__NguoiDun__A9D105349AF14364").IsUnique();

            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LinkAnhDaiDien).IsUnicode(false);
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Ten).HasMaxLength(100);
            entity.Property(e => e.TinhTrang).HasMaxLength(50);
            entity.Property(e => e.VaiTro).HasMaxLength(20);
        });

        modelBuilder.Entity<TheLoai>(entity =>
        {
            entity.HasKey(e => e.MaTheLoai).HasName("PK__TheLoai__D73FF34ACBF97CDA");

            entity.ToTable("TheLoai");

            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<TienDo>(entity =>
        {
            entity.HasKey(e => e.MaTienDo).HasName("PK__TienDo__C5D04CAE7362AD2B");

            entity.ToTable("TienDo");

            entity.Property(e => e.NgayThamGia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhanTramTienDo).HasDefaultValue(0.0);

            entity.HasOne(d => d.MaKhoaHocNavigation).WithMany(p => p.TienDos)
                .HasForeignKey(d => d.MaKhoaHoc)
                .HasConstraintName("FK__TienDo__MaKhoaHo__6D0D32F4");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.TienDos)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK__TienDo__MaNguoiD__6E01572D");
        });

        modelBuilder.Entity<TienDoBaiHoc>(entity =>
        {
            entity.HasKey(e => e.MaTienDoBaiHoc).HasName("PK__TienDoBa__299D1CC652327FD5");

            entity.ToTable("TienDoBaiHoc");

            entity.Property(e => e.DaHoanThanh).HasDefaultValue(false);
            entity.Property(e => e.LanCuoiXem).HasColumnType("datetime");

            entity.HasOne(d => d.MaBaiHocNavigation).WithMany(p => p.TienDoBaiHocs)
                .HasForeignKey(d => d.MaBaiHoc)
                .HasConstraintName("FK__TienDoBai__MaBai__76969D2E");

            entity.HasOne(d => d.MaTienDoNavigation).WithMany(p => p.TienDoBaiHocs)
                .HasForeignKey(d => d.MaTienDo)
                .HasConstraintName("FK__TienDoBai__MaTie__787EE5A0");
        });

        modelBuilder.Entity<VwKhoaHocGiaThucTe>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_KhoaHoc_GiaThucTe");

            entity.Property(e => e.GiaGoc).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime");
            entity.Property(e => e.TieuDe).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
