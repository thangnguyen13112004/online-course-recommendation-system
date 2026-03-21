using Neo4j.Driver;
using online_course_recommendation_system.Configurations;
using Microsoft.EntityFrameworkCore;
using online_course_recommendation_system.Data;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký EF Core kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// bind config
builder.Services.Configure<Neo4jSettings>(
    builder.Configuration.GetSection("Neo4j")
);

// đăng ký IDriver singleton
builder.Services.AddSingleton<IDriver>(sp =>
{
    var config = builder.Configuration.GetSection("Neo4j").Get<Neo4jSettings>()
                 ?? throw new Exception("Neo4j configuration is missing.");

    return GraphDatabase.Driver(
        config.Uri,
        AuthTokens.Basic(config.Username, config.Password)
    );
});

// 1. Thêm các Controllers vào hệ thống
builder.Services.AddControllers();

// 2. KHÚC NÀY ĐỂ BẬT SWAGGER NÈ
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. CẤU HÌNH HIỂN THỊ GIAO DIỆN SWAGGER
// Thường chỉ bật Swagger khi đang code (Development) để bảo mật
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gợi ý Khóa học v1");
        c.RoutePrefix = string.Empty; // Mở web lên là hiện luôn Swagger ở trang chủ (localhost:port)
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();