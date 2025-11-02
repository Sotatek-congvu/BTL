using Events;
using Helper;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/reading")]
public class ReadingController : ControllerBase
{
    private readonly string _geminiKey = HttpContextHelper.GetSecretKey();
    private readonly ILogger _logger;
    public ReadingController(ILogger logger)
    {
        _logger = logger;
    }

    public record GenerateReq(string Level, string Topic, int TargetWords = 220, string Locale = "en");

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateReq req)
    {
        var keyRaw = JsonSerializer.Serialize(req);
        var key = "reading:" + ReadingTopic.Sha256(keyRaw);

       

        // Sinh reading text từ model
        var text = await ReadingTopic.GenerateReadingAsync(
            _geminiKey,
            req.Level,
            req.Topic,
            req.TargetWords,
            req.Locale
        );

        // Cache kết quả
        return Content(text, "text/plain", Encoding.UTF8);
    }

   


    
}
