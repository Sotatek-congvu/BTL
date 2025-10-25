using EngAce.Api.DTO;
using Entities.Enums;
using Events;
using Helper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace EngAce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpeakingAiController : ControllerBase
    {
        private readonly string _geminiKey = HttpContextHelper.GetSecretKey();

        // ===============================================
        // 1️⃣ CHẤM PHÁT ÂM (so sánh với câu mẫu)
        // ===============================================
        [HttpPost("AnalyzeSpeech")]
        [Consumes("multipart/form-data")]
        [Produces("text/plain")]
        public async Task<IActionResult> AnalyzeSpeech([FromForm] AnalyzeSpeechRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Audio file required.");

            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);
            string mimeType = request.File.ContentType ?? "audio/wav";

            string result = await SpeakingTopic.AnalyzeSpeechAsync(
                _geminiKey,
                request.Topic,
                request.Reference,
                ms.ToArray(),
                mimeType
            );

            return Content(result, "text/plain", Encoding.UTF8);
        }

        // ===============================================
        // 2️⃣ SINH HỘI THOẠI SONG NGỮ (Gemini JSON)
        // ===============================================
        [HttpPost("GenerateDialogue")]
        [Produces("application/json")]
        public async Task<IActionResult> GenerateDialogue(
            [FromForm] string topic,
            [FromForm] string purpose = "Speaking practice",
            [FromForm] EnglishLevel level = EnglishLevel.Elementary)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return BadRequest("Topic is required.");

            var json = await SpeakingTopic.GenerateDialogueAsync(
                _geminiKey,
                "English-Vietnamese",
                level,
                purpose,
                topic
            );

            return Content(json, "application/json", Encoding.UTF8);
        }

        // ===============================================
        // 3️⃣ CHẤM ĐIỂM CÂU TRẢ LỜI (AI Evaluate)
        // ===============================================
        [HttpPost("EvaluateAnswer")]
        [Consumes("multipart/form-data")]
        [Produces("text/plain")]
        public async Task<IActionResult> EvaluateAnswer([FromForm] EvaluateAnswerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Topic))
                return BadRequest("Topic is required.");

            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question is required.");

            string learnerAnswer = request.AnswerText ?? "(audio)";
            byte[]? audioBytes = null;
            string mimeType = "audio/wav";

            if (request.File != null && request.File.Length > 0)
            {
                using var ms = new MemoryStream();
                await request.File.CopyToAsync(ms);
                audioBytes = ms.ToArray();
                mimeType = request.File.ContentType ?? "audio/wav";
            }

            string result = await SpeakingTopic.EvaluateAnswerAsync(
                _geminiKey,
                request.Topic,
                request.Question,
                learnerAnswer,
                audioBytes,
                mimeType
            );

            return Content(result, "text/plain", Encoding.UTF8);
        }

        // ===============================================
        // 4️⃣ LẤY CÂU TIẾP THEO TRONG JSON HỘI THOẠI
        // ===============================================
        [HttpPost("NextDialogue")]
        [Produces("application/json")]
        public IActionResult NextDialogue([FromBody] NextDialogueRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DialogueJson))
                return BadRequest("Dialogue JSON required.");

            try
            {
                var doc = JsonDocument.Parse(request.DialogueJson);
                var dialogues = doc.RootElement.GetProperty("dialogue").EnumerateArray().ToList();
                var current = dialogues.FirstOrDefault(x => x.GetProperty("id").GetInt32() == request.CurrentId);

                var next = dialogues.FirstOrDefault(x => x.GetProperty("id").GetInt32() == request.CurrentId + 1);
                if (next.ValueKind == JsonValueKind.Undefined)
                    return Ok(new { done = true, message = "End of dialogue." });

                return Ok(new
                {
                    done = false,
                    nextId = next.GetProperty("id").GetInt32(),
                    en = next.GetProperty("en").GetString(),
                    vi = next.GetProperty("vi").GetString(),
                    speaker = next.GetProperty("speaker").GetString()
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Invalid JSON format: {ex.Message}");
            }
        }
    }
}
