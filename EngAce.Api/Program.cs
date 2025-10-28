using EngAce.Api.Cached;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
var builder = WebApplication.CreateBuilder(args);
var redisConfig = new ConfigurationOptions
{
    EndPoints = { { "redis-13910.crce185.ap-seast-1-1.ec2.redns.redis-cloud.com", 13910 } },
    User = "default",
    Password = "TZQbYDYpnsvGAcOee52YhveGO7ug7Apn",
    AbortOnConnectFail = false,
    ConnectTimeout = 8000,
    SyncTimeout = 8000
};

// ✅ 2. Khởi tạo ConnectionMultiplexer dùng Singleton
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConfig)
);
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Thêm Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "API Demo with Swagger"
    });
});

builder.Services.AddMemoryCache();

var app = builder.Build();

// Cấu hình middleware
if (app.Environment.IsDevelopment())
{
    // Bật Swagger UI trong môi trường dev
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");

app.MapControllers();

app.Run();
