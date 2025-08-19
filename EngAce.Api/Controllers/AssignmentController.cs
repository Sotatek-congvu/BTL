using CDK.Api.DTO;
using Entities;
using Entities.Enums;
using Events;
using Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CDK.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssignmentController(IMemoryCache cache, ILogger<AssignmentController> logger) : ControllerBase
{
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<AssignmentController> _logger = logger;
    private readonly string _accessKey = HttpContextHelper.GetSecretKey();

    [HttpPost("Generate")]
    public async Task<ActionResult<List<Quiz>>> Generate([FromBody] GenerateQuizzes request)
    {
        if (string.IsNullOrEmpty(_accessKey))
        {
            return Unauthorized("Invalid Access Key");
        }

        request.Topic = string.IsNullOrEmpty(request.Topic) ? "" : request.Topic.Trim();

        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest("Tên chủ đề không được để trống");
        }

        if (GeneralHelper.GetTotalWords(request.Topic) > QuizScope.MaxTotalWordsOfTopic)
        {
            return BadRequest($"Chủ đề không được chứa nhiều hơn {QuizScope.MaxTotalWordsOfTopic} từ");
        }

        if (request.TotalQuestions < QuizScope.MinTotalQuestions || request.TotalQuestions > QuizScope.MaxTotalQuestions)
        {
            return BadRequest($"Số lượng câu hỏi phải nằm trong khoảng {QuizScope.MinTotalQuestions} đến {QuizScope.MaxTotalQuestions}");
        }

        if (request.TotalQuestions < request.AssignmentTypes.Count)
        {
            return BadRequest($"Số lượng câu hỏi không được nhỏ hơn số dạng câu hỏi mà bạn chọn");
        }

        var cacheKey = $"GenerateQuiz-{request.Topic.ToLower()}-{string.Join(string.Empty, request.AssignmentTypes)}-{request.EnglishLevel}-{request.TotalQuestions}";
        if (_cache.TryGetValue(cacheKey, out var cachedQuizzes))
        {
            return Ok(cachedQuizzes);
        }

        try
        {
            var quizzes = await QuizScope.GenerateQuizes(_accessKey, request.Topic, request.AssignmentTypes, request.EnglishLevel, request.TotalQuestions);
            _cache.Set(cacheKey, quizzes, TimeSpan.FromMinutes(request.TotalQuestions));

            _logger.LogInformation("{_accessKey} generated: {Topic} - Quizz Types: {Types}", _accessKey[..10], request.Topic, string.Join("-", request.AssignmentTypes.Select(t => t.ToString())));

            return Created("Success", quizzes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Topic: {Topic} - Quizz Types: {Types}", request.Topic, string.Join("-", request.AssignmentTypes.Select(t => t.ToString())));
            return BadRequest(ex.Message);
        }
    }

}