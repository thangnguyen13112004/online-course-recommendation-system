-- Tạo Database
CREATE DATABASE ELearning_DB;
GO
USE ELearning_DB;
GO

-- USE master;
-- GO

-- ALTER DATABASE ELearning_DB 
-- SET SINGLE_USER 
-- WITH ROLLBACK IMMEDIATE;

-- DROP DATABASE ELearning_DB;


-- ==========================================
-- 1. TẠO CÁC BẢNG ĐỘC LẬP (KHÔNG CÓ KHÓA NGOẠI)
-- ==========================================

-- Bảng Người dùng
CREATE TABLE NguoiDung (
    MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,
    Ten NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    MatKhau VARCHAR(255) NOT NULL,
    VaiTro NVARCHAR(20) CHECK (VaiTro IN ('HocVien','GiaoVien','Admin')),
    LinkAnhDaiDien VARCHAR(MAX),
    TieuSu NVARCHAR(MAX),
    TinhTrang NVARCHAR(50),
    NgayTao DATETIME DEFAULT GETDATE()
);

-- Bảng Khuyến mãi
CREATE TABLE KhuyenMai (
    MaKhuyenMai INT IDENTITY(1,1) PRIMARY KEY,
    TenChuongTrinh NVARCHAR(200),
    PhanTramGiam FLOAT,
    NgayBatDau DATETIME,
    NgayKetThuc DATETIME
);

-- Bảng Thể loại
CREATE TABLE TheLoai (
    MaTheLoai INT IDENTITY(1,1) PRIMARY KEY,
    Ten NVARCHAR(100) NOT NULL,
    MoTa NVARCHAR(MAX)
);

-- ==========================================
-- 2. TẠO CÁC BẢNG CÓ PHỤ THUỘC BẬC 1
-- ==========================================

-- Bảng Giỏ hàng
CREATE TABLE GioHang (
    MaGioHang INT IDENTITY(1,1) PRIMARY KEY,
    NgayTao DATETIME DEFAULT GETDATE(),
    MaNguoiDung INT FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung)
);

-- Bảng Hóa đơn
CREATE TABLE HoaDon (
    MaHoaDon INT IDENTITY(1,1) PRIMARY KEY,
    TongTien DECIMAL(18,2) NOT NULL,
    PhuongThucThanhToan NVARCHAR(100),
    TinhTrangThanhToan NVARCHAR(100),
    NgayTao DATETIME DEFAULT GETDATE(),
    MaNguoiDung INT FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung)
);

-- Bảng Khóa học (Trung tâm)
CREATE TABLE KhoaHoc (
    MaKhoaHoc INT IDENTITY(1,1) PRIMARY KEY,
    TieuDe NVARCHAR(255) NOT NULL,
    TieuDePhu NVARCHAR(255),
    MoTa NVARCHAR(MAX),
    GiaGoc DECIMAL(18,2) CHECK (GiaGoc >= 0),
    TinhTrang NVARCHAR(50),
    TBDanhGia FLOAT DEFAULT 0 CHECK (TBDanhGia >= 0 AND TBDanhGia <= 5),
    NgayTao DATETIME DEFAULT GETDATE(),
    NgayCapNhat DATETIME,
    MaTheLoai INT FOREIGN KEY REFERENCES TheLoai(MaTheLoai),
    KiNang NVARCHAR(MAX),
    AnhURL VARCHAR(MAX),
    MaKhuyenMai INT FOREIGN KEY REFERENCES KhuyenMai(MaKhuyenMai)
);

-- ==========================================
-- 3. TẠO CÁC BẢNG CÓ PHỤ THUỘC BẬC 2
-- ==========================================

CREATE TABLE GiangVien_KhoaHoc (
    MaKhoaHoc INT FOREIGN KEY REFERENCES KhoaHoc(MaKhoaHoc),
    MaGiangVien INT FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung),
    LaGiangVienChinh BIT DEFAULT 0, 
    TyLeDoanhThu FLOAT DEFAULT 0 CHECK (TyLeDoanhThu >= 0 AND TyLeDoanhThu <= 100), 
    PRIMARY KEY (MaKhoaHoc, MaGiangVien) 
);

-- Bảng Chi tiết giỏ hàng (Items trong Giỏ hàng)
CREATE TABLE ChiTietGioHang (
    MaChiTietGioHang INT IDENTITY(1,1) PRIMARY KEY,
    Gia DECIMAL(18,2),
    MaKhoaHoc INT FOREIGN KEY REFERENCES KhoaHoc(MaKhoaHoc),
    MaGioHang INT FOREIGN KEY REFERENCES GioHang(MaGioHang),
	CONSTRAINT UQ_GioHang_KhoaHoc UNIQUE(MaGioHang, MaKhoaHoc)
);

-- Bảng Chi tiết hóa đơn
CREATE TABLE ChiTietHoaDon (
    MaChiTietHoaDon INT IDENTITY(1,1) PRIMARY KEY,
    Gia DECIMAL(18,2),
    MaHoaDon INT FOREIGN KEY REFERENCES HoaDon(MaHoaDon),
    MaKhoaHoc INT FOREIGN KEY REFERENCES KhoaHoc(MaKhoaHoc)
);

-- Bảng Đánh giá
CREATE TABLE DanhGia (
    MaDanhGia INT IDENTITY(1,1) PRIMARY KEY,
    Rating FLOAT CHECK (Rating >= 0 AND Rating <= 5),
    BinhLuan NVARCHAR(MAX),
    NgayDanhGia DATETIME DEFAULT GETDATE(),
    Thich INT DEFAULT 0,
    MaKhoaHoc INT FOREIGN KEY REFERENCES KhoaHoc(MaKhoaHoc),
    MaNguoiDung INT FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung)
);

-- Bảng Chứng chỉ
CREATE TABLE ChungChi (
    MaChungChi INT IDENTITY(1,1) PRIMARY KEY,
    NgayPhat DATETIME DEFAULT GETDATE(),
    MaKhoaHoc INT FOREIGN KEY REFERENCES KhoaHoc(MaKhoaHoc),
    MaNguoiDung INT FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung)
);

-- Bảng Tiến độ
CREATE TABLE TienDo (
    MaTienDo INT IDENTITY(1,1) PRIMARY KEY,
    NgayThamGia DATETIME DEFAULT GETDATE(),
    PhanTramTienDo FLOAT DEFAULT 0 CHECK (PhanTramTienDo >= 0 AND PhanTramTienDo <= 100),
    TinhTrang NVARCHAR(50),
    MaKhoaHoc INT FOREIGN KEY REFERENCES KhoaHoc(MaKhoaHoc),
    MaNguoiDung INT FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung) 
);

-- Bảng Chương
CREATE TABLE Chuong (
    MaChuong INT IDENTITY(1,1) PRIMARY KEY,
    TieuDe NVARCHAR(255) NOT NULL,
    MaKhoaHoc INT FOREIGN KEY REFERENCES KhoaHoc(MaKhoaHoc)
);

-- ==========================================
-- 4. TẠO CÁC BẢNG CÓ PHỤ THUỘC BẬC 3 & 4
-- ==========================================

-- Bảng Bài học
CREATE TABLE BaiHoc (
    MaBaiHoc INT IDENTITY(1,1) PRIMARY KEY,
    LinkVideo VARCHAR(MAX),
    MaChuong INT FOREIGN KEY REFERENCES Chuong(MaChuong),
    BaiTap NVARCHAR(MAX),
    LyThuyet NVARCHAR(MAX)
);

-- Bảng Tiến độ bài học
CREATE TABLE TienDoBaiHoc (
    MaTienDoBaiHoc INT IDENTITY(1,1) PRIMARY KEY,
    MaBaiHoc INT FOREIGN KEY REFERENCES BaiHoc(MaBaiHoc),
    DaHoanThanh BIT DEFAULT 0, -- Tương ứng với 0/1 trong hình
    LanCuoiXem DATETIME,
    ThoiGian INT, -- Lưu số phút hoặc số giây
	MaTienDo INT FOREIGN KEY REFERENCES TienDo(MaTienDo)
);
GO