using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Thêm hỗ trợ cho Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddMemoryCache();
var app = builder.Build();

// Cấu hình pipeline
app.UseRouting();

// Ánh xạ các endpoint của Controllers
app.MapControllers();

// Chạy ứng dụng
app.Run();