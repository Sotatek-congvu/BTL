using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Thêm hỗ trợ cho Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Thêm dịch vụ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()    // Cho phép tất cả các origin (dùng trong dev/test)
              .AllowAnyMethod()    // Cho phép tất cả các HTTP method (GET, POST, v.v.)
              .AllowAnyHeader();   // Cho phép tất cả các header
    });
    // Nếu chỉ muốn cho phép origin cụ thể (ví dụ: GitHub Pages), thay bằng:
    // options.AddPolicy("AllowSpecific", policy =>
    // {
    //     policy.WithOrigins("https://<username>.github.io") // Thay bằng URL của bạn
    //           .AllowAnyMethod()
    //           .AllowAnyHeader();
    // });
});

builder.Services.AddMemoryCache();

var app = builder.Build();

// Cấu hình pipeline
app.UseRouting();

// Thêm middleware CORS trước MapControllers
app.UseCors("AllowAll"); // Dùng chính sách "AllowAll"
// Nếu dùng chính sách cụ thể: app.UseCors("AllowSpecific");

app.MapControllers();

// Chạy ứng dụng
app.Run();