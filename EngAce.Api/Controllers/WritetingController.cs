using EngAce.Api.DTO;
using Entities.Enums;
using Events;
using Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace EngAce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WritingAiController(IMemoryCache cache, ILogger<WritingAiController> logger) : ControllerBase
{
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<WritingAiController> _logger = logger;
    private readonly string _accessKey = HttpContextHelper.GetSecretKey();

    [HttpPost("GenerateTopic")]
    public async Task<ActionResult<string>> GenerateTopic([FromBody] WritingTopicRequest request)
    {
        if (string.IsNullOrEmpty(_accessKey))
            return Unauthorized("Invalid Access Key");

        if (string.IsNullOrWhiteSpace(request.Topic))
            return BadRequest("Chủ đề không được để trống.");

        var cacheKey = $"AIWriting-{request.Language}-{request.Level}-{request.Purpose}-{request.Topic}";
        if (_cache.TryGetValue(cacheKey, out string cached))
        {
            _logger.LogInformation("Returning cached AI writing topic: {Topic}", request.Topic);
            return Ok(cached);
        }

        try
        {
            // 🔹 Gọi AI sinh hội thoại + gợi ý học tập
            var result = await WritetingTopic.GenerateDialogueAsync(
                _accessKey,
                request.Language,
                request.Level,
                request.Purpose,
                request.Topic
            );

            _cache.Set(cacheKey, result, TimeSpan.FromHours(2));
            _logger.LogInformation("AI generated writing topic for {Topic} - Level: {Level}", request.Topic, request.Level);

            return Created("Success", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI topic for {Topic}", request.Topic);
            return BadRequest($"Không thể tạo bài luyện viết: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi bài viết để AI chấm điểm và nhận feedback chi tiết.
    /// </summary>
    [HttpPost("Review")]
    public async Task<ActionResult<string>> Review([FromBody] WritingReviewRequest request)
    {
        if (string.IsNullOrEmpty(_accessKey))
            return Unauthorized("Invalid Access Key");

        if (string.IsNullOrWhiteSpace(request.Requirement))
            return BadRequest("Phần yêu cầu đề bài không được để trống.");

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Nội dung bài viết không được để trống.");

        var cacheKey = $"WritingReview-{request.Level}-{request.Requirement.GetHashCode()}-{request.Content.GetHashCode()}";
        if (_cache.TryGetValue(cacheKey, out string cachedReview))
        {
            _logger.LogInformation("Returning cached writing review for key: {CacheKey}", cacheKey);
            return Ok(cachedReview);
        }

        try
        {
            var review = await WritetingTopic.GenerateReview(_accessKey, request.Level, request.Requirement, request.Content);

            _cache.Set(cacheKey, review, TimeSpan.FromMinutes(30));
            _logger.LogInformation("AI reviewed writing for level {Level}", request.Level);

            return Created("Success", review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating writing review for level {Level}", request.Level);
            return BadRequest($"Đã xảy ra lỗi khi đánh giá bài viết: {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy danh sách trình độ tiếng Anh (CEFR)
    /// </summary>
    [HttpGet("GetEnglishLevels")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
    public ActionResult<Dictionary<int, string>> GetEnglishLevels()
    {
        var descriptions = Enum.GetValues(typeof(EnglishLevel))
            .Cast<EnglishLevel>()
            .ToDictionary(level => (int)level, level => GeneralHelper.GetEnumDescription(level));

        return Ok(descriptions);
    }
}