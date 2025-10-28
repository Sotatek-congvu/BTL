using Events;
using Helper;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using EngAce.Api.Cached; // nơi chứa ICacheService

[ApiController]
[Route("api/reading")]
public class ReadingController : ControllerBase
{
    private readonly ICacheService _cache;
    private readonly string _geminiKey = HttpContextHelper.GetSecretKey();

    public ReadingController(ICacheService cache)
    {
        _cache = cache;
    }

    public record GenerateReq(string Level, string Topic, int TargetWords = 220, string Locale = "en");

    // 1️⃣ Tạo đoạn văn reading và cache kết quả (key = JSON input, value = text)
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateReq req)
    {
        var keyRaw = JsonSerializer.Serialize(req);
        var key = "reading:" + ReadingTopic.Sha256(keyRaw);

        // Kiểm tra cache
        var cached = await _cache.GetAsync(key);
        if (!string.IsNullOrEmpty(cached))
            return Content(cached, "text/plain", Encoding.UTF8);

        // Sinh reading text từ model
        var text = await ReadingTopic.GenerateReadingAsync(
            _geminiKey,
            req.Level,
            req.Topic,
            req.TargetWords,
            req.Locale
        );

        // Cache kết quả
        await _cache.SetAsync(key, text, TimeSpan.FromDays(1));
        return Content(text, "text/plain", Encoding.UTF8);
    }

    
}
