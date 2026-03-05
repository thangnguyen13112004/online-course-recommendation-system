var builder = WebApplication.CreateBuilder(args);

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