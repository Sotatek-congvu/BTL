using Entities;
using Entities.Enums;
using Events;
using Helper;
using Microsoft.AspNetCore.Mvc;

namespace CDK.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TopicController(ILogger<TopicController> logger) : ControllerBase
{
    private readonly ILogger<TopicController> _logger = logger;
    private readonly string _accessKey = HttpContextHelper.GetSecretKey();

    [HttpPost("GenerateAnswer")]
    public async Task<ActionResult<string>> GenerateAnswer([FromBody] Conversation request, string username, string gender, sbyte age, EnglishLevel englishLevel, bool enableReasoning = false, bool enableSearching = false)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return Ok("Gửi vội vậy bé yêu! Chưa nhập câu hỏi kìa.");
        }

        if (GeneralHelper.GetTotalWords(request.Question) > 30)
        {
            return Ok("Hỏi ngắn thôi bé yêu, bộ mắc hỏi quá hay gì 💢\nHỏi câu nào dưới 30 từ thôi, để thời gian cho anh suy nghĩ với chứ.");
        }

        try
        {
            var result = await TopicScope.GenerateAnswer(_accessKey, request, username, gender, age, englishLevel, enableReasoning, enableSearching,request.Topic);

            _logger.LogInformation($"{_accessKey[..10]} ({username}) asked (Reasoning: {enableReasoning} - Grounding: {enableSearching}): {request.Question}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot generate answer");
            return Ok("Hoi từ từ thôi bé yêu, bộ mắc đi đẻ quá hay gì 💢\nNgồi đợi 1 phút cho anh đi uống ly cà phê đã.");
        }
    }
}