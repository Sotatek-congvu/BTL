using Events;
using Helper;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using EngAce.Api.Cached;
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

    // A) Chỉ sinh reading (như trước)
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateReq req)
    {
        var key = BuildKey("reading:pc", req.Level, req.Topic, req.TargetWords, req.Locale, 1);
        var cached = await _cache.GetAsync(key);
        if (!string.IsNullOrEmpty(cached))
            return Content(cached, "application/json", Encoding.UTF8);

        var json = await ReadingTopic.GenerateReadingAsync(_geminiKey, req.Level, req.Topic, req.TargetWords, req.Locale);

        if (!IsValidReadingJson(json)) return BadRequest("Invalid JSON from model.");
        await _cache.SetAsync(key, json, TimeSpan.FromDays(14));
        return Content(json, "application/json", Encoding.UTF8);
    }

    // B) Sinh reading + TOEIC questions (XML) trong 1 call
    [HttpPost("generate-with-questions")]
    public async Task<IActionResult> GenerateWithQuestions([FromBody] GenerateReq req)
    {
        // 1) Reading (cache đề)
        var readingKey = BuildKey("reading:pc", req.Level, req.Topic, req.TargetWords, req.Locale, 1);
        var readingJson = await _cache.GetAsync(readingKey);
        if (string.IsNullOrEmpty(readingJson))
        {
            readingJson = await ReadingTopic.GenerateReadingAsync(_geminiKey, req.Level, req.Topic, req.TargetWords, req.Locale);
            if (!IsValidReadingJson(readingJson)) return BadRequest("Invalid reading JSON.");
            await _cache.SetAsync(readingKey, readingJson, TimeSpan.FromDays(14));
        }

        // 2) Extract passage plaintext
        var passage = ReadingTopic.ExtractPassageFromReadingJson(readingJson);
        if (string.IsNullOrWhiteSpace(passage)) return BadRequest("Cannot extract passage text.");

        // 3) TOEIC Questions (cache theo passage + level)
        var qKey = BuildQuestionsKey(passage, req.Level);
        var questionsXml = await _cache.GetAsync(qKey);
        if (string.IsNullOrEmpty(questionsXml))
        {
            questionsXml = await ReadingTopic.GenerateToeicQuestionsAsync(_geminiKey, passage, req.Level);
            // Optional: validate tối thiểu bằng check 3 tag chính
            if (!questionsXml.Contains("<comprehension_questions>") ||
                !questionsXml.Contains("<answer_key>") ||
                !questionsXml.Contains("<scoring_rubric>"))
                return BadRequest("Invalid questions XML.");
            await _cache.SetAsync(qKey, questionsXml, TimeSpan.FromDays(14));
        }

        // 4) Trả hợp nhất: reading JSON + questions XML (string)
        return Ok(new
        {
            reading = JsonSerializer.Deserialize<JsonElement>(readingJson),
            toeicQuestionsXml = questionsXml
        });
    }

    private static bool IsValidReadingJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return root.TryGetProperty("meta", out _) &&
                   root.TryGetProperty("text", out var text) &&
                   text.TryGetProperty("paragraphs", out _);
        }
        catch { return false; }
    }

    private static string BuildKey(string prefix, string level, string topic, int words, string locale, int schemaV)
    {
        var raw = $"model=gemini-1.5-flash|{level}|{topic}|{words}|{locale}|schemaV={schemaV}";
        return $"{prefix}:{ReadingTopic.Sha256(raw)}";
        // ví dụ: reading:pc:AB12...
    }

    private static string BuildQuestionsKey(string passage, string level)
    {
        var raw = $"toeicq|{level}|{passage}";
        return $"reading:toeicq:{ReadingTopic.Sha256(raw)}";
    }
}
