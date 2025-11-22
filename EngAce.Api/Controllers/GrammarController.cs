using Entities.Enums;
using Events;
using Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace EngAce.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GrammarController(IMemoryCache cache, ILogger<GrammarController> logger) : ControllerBase
{
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<GrammarController> _logger = logger;
    private readonly string _accessKey = HttpContextHelper.GetSecretKey();

    [HttpGet("Generate")]
    public async Task<ActionResult<string>> Generate(string titleLesson)
    {
        // ✅ Kiểm tra key hợp lệ
        if (string.IsNullOrEmpty(_accessKey))
        {
            return Unauthorized("Invalid Access Key");
        }

        // ✅ Kiểm tra đầu vào
        if (string.IsNullOrWhiteSpace(titleLesson))
        {
            return BadRequest("Không được để trống tiêu đề bài học (titleLesson).");
        }

        titleLesson = titleLesson.Trim();
        var cacheKey = $"Grammar-{titleLesson}";

        // ✅ Kiểm tra cache trước
        if (_cache.TryGetValue(cacheKey, out string cachedResult))
        {
            _logger.LogInformation("AccessKey: {AccessKey} retrieved cached grammar theory for: {TitleLesson}",
                _accessKey[..10], titleLesson);
            return Ok(cachedResult);
        }

        try
        {
            var result = await GrammarTheoryGenerator.GenerateTheoryAsync(_accessKey, titleLesson);

            _cache.Set(cacheKey, result, TimeSpan.FromDays(5));

            // ✅ Ghi log hoạt động
            _logger.LogInformation("AccessKey: {AccessKey} generated grammar theory for: {TitleLesson}",
                _accessKey[..10], titleLesson);

            // ✅ Trả về kết quả
            return Created("Success", new
            {
                title = titleLesson,
                theory = result.Trim()
            });
        }
        catch (Exception ex)
        {
            // ❌ Ghi log lỗi
            _logger.LogError(ex, "Error generating grammar theory for lesson: {TitleLesson}", titleLesson);

            // 🧋 Fallback message thân thiện
            return Created("Success", new
            {
                title = titleLesson,
                message = "## CẢNH BÁO\nEngAce đang pha cà phê nên tạm thời vắng mặt ☕. Bạn hiền thử lại sau vài phút nhé!"
            });
        }
    }
}
