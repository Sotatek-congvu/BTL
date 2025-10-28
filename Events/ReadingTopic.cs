using Gemini.NET;
using Models.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Events;

public static class ReadingTopic
{
    // ====== (1) System instruction sinh bài đọc (giữ nguyên như trước) ======
    public const string Instruction = @"
Bạn là biên tập viên ESL. Hãy sinh một bài Luyện Đọc theo đúng CEFR và chủ đề yêu cầu.
Chỉ trả về **JSON hợp lệ 100%** theo SCHEMA cố định dưới đây, **không thêm chữ nào ngoài JSON**.

SCHEMA JSON:
{
  ""meta"": {
    ""level"": ""A1|A2|B1|B2|C1"",
    ""topic"": ""string"",
    ""title"": ""string"",
    ""words"": number,
    ""schema_version"": 1
  },
  ""pre_reading"": {
    ""warmup_questions"": [""string"", ""string""],
    ""key_vocab"": [
      { ""word"": ""string"", ""pos"": ""string?"", ""meaning_vi"": ""string"", ""example_en"": ""string"" }
    ]
  },
  ""text"": {
    ""paragraphs"": [""string"", ""string""]
  },
  ""post_reading"": {
    ""quizzes"": [
      { ""type"": ""mcq"", ""question"": ""string"", ""options"": [""A"", ""B"", ""C"", ""D""], ""correct"": ""A|B|C|D"", ""explanation"": ""string"" },
      { ""type"": ""tf"",  ""question"": ""string"", ""answer"": true,  ""explanation"": ""string"" },
      { ""type"": ""gapfill"", ""question"": ""string"", ""answer"": ""string"" }
    ],
    ""summary_task"": ""string"",
    ""vocab_review"": [""string"", ""string""]
  }
}
";

    // ====== (2) System instruction sinh bộ câu hỏi TOEIC (XML) ======
    public const string ToeicQuestionsInstruction = @"
You are an expert TOEIC Reading Comprehension test designer.
Read the passage, analyze its vocabulary, structure, and themes, then generate questions suitable for the specified TOEIC/CEFR level.

Level mapping:
- A1: literal comprehension only (3–4 MCQ)
- A2: 4–5 MCQ, include 1–2 simple inference/context items
- B1: 5–6 items (mix MCQ + short answer), inference, vocab-in-context, author's purpose

Scoring:
- Each correct = 1 point
- Provide a rubric with total and performance bands

Output MUST be **XML only**, strictly this structure:

<comprehension_questions>
[Level-appropriate questions; MCQ include options; B1 may include short-answer]
</comprehension_questions>

<answer_key>
[Correct answers with brief explanations]
</answer_key>

<scoring_rubric>
- Total Questions: [N]
- Each correct answer = 1 point
- 90–100% = Excellent
- 70–89%  = Good
- <70%    = Needs Improvement
</scoring_rubric>
";

    // ====== (3) Generate Reading JSON ======
    public static async Task<string> GenerateReadingAsync(
        string apiKey,
        string level,
        string topic,
        int targetWords = 220,
        string locale = "en",
        int schemaVersion = 1,
        float temperature = 0.5f
    )
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Yêu cầu sinh bài đọc theo thông số sau:");
        prompt.AppendLine($"- CEFR level: {level}");
        prompt.AppendLine($"- Topic: {topic}");
        prompt.AppendLine($"- Target words (±15%): {targetWords}");
        prompt.AppendLine($"- Locale: {locale}");
        prompt.AppendLine($"- Schema version: {schemaVersion}");
        prompt.AppendLine();
        prompt.AppendLine("Quy tắc:");
        prompt.AppendLine("- Câu và từ vựng phù hợp đúng CEFR level.");
        prompt.AppendLine("- Độ dài ~targetWords (±15%).");
        prompt.AppendLine("- key_vocab 8–12 mục, meaning_vi ngắn gọn, example_en tự nhiên.");
        prompt.AppendLine("- quizzes gồm 4–6 câu (ít nhất 1 mcq, 1 tf, 1 gapfill).");
        prompt.AppendLine("- Chỉ trả JSON, không markdown, không lời giải thích kèm theo.");

        var generator = new Generator(apiKey);
        var apiRequest = new ApiRequestBuilder()
            .WithSystemInstruction(Instruction)
            .WithPrompt(prompt.ToString())
            .WithDefaultGenerationConfig(temperature, ResponseMimeType.PlainText)
            .DisableAllSafetySettings()
            .Build();

        var response = await generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash_Thinking);
        return response.Result; // JSON string
    }

    // ====== (4) Generate TOEIC questions (XML) từ passage & level ======
    public static async Task<string> GenerateToeicQuestionsAsync(
        string apiKey,
        string passagePlainText,
        string level,
        float temperature = 0.6f
    )
    {
        var prompt = $"Level: {level}\nReading Passage:\n{passagePlainText}";

        var generator = new Generator(apiKey);
        var apiRequest = new ApiRequestBuilder()
            .WithSystemInstruction(ToeicQuestionsInstruction)
            .WithPrompt(prompt)
            .WithDefaultGenerationConfig(temperature, ResponseMimeType.PlainText)
            .DisableAllSafetySettings()
            .Build();

        var response = await generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash_Thinking);
        return response.Result; // XML string
    }

    // ====== (5) Helper: rút passage thuần từ JSON reading ======
    public static string ExtractPassageFromReadingJson(string readingJson)
    {
        using var doc = JsonDocument.Parse(readingJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("text", out var text) ||
            !text.TryGetProperty("paragraphs", out var paras) ||
            paras.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var p in paras.EnumerateArray())
        {
            var line = p.GetString();
            if (!string.IsNullOrWhiteSpace(line))
                sb.AppendLine(line.Trim());
        }
        return sb.ToString().Trim();
    }

    // ====== (6) Helper: hash key tiện cache ======
    public static string Sha256(string s)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
    }
}
