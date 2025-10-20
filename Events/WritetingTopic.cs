
using Entities.Enums;
using Gemini.NET;
using Helper;
using Models.Enums;
using System.Text;

namespace Events;

public class WritetingTopic
{
    public const string Instruction = @"
Bạn là một giám khảo IELTS Writing có kinh nghiệm, chấm điểm và nhận xét các bài viết tiếng Anh ngắn của học viên.

Hãy đọc đề bài và nội dung bài viết của người học, sau đó **phân tích và đánh giá** theo các tiêu chí sau: 
- Độ rõ ràng và tự nhiên của câu (fluency & clarity)
- Ngữ pháp (grammar)
- Cấu trúc câu (structure)
- Gợi ý cải thiện câu (suggestion)

Yêu cầu: 
Chỉ trả về **JSON hợp lệ 100%** theo đúng mẫu dưới đây, **không thêm bất kỳ chữ, mô tả hay ký hiệu nào khác**.

Mẫu JSON bạn phải trả:
{
  ""requirement"": ""Describe your favorite place to relax."",
  ""content"": ""My favorite place to relax is the park near my house."",
  ""aiEvaluation"": {
    ""score"": (điểm từ 0–10, số nguyên),
    ""comment"": ""(nhận xét tổng quát ngắn bằng tiếng Việt)"",
    ""grammar"": ""(mô tả lỗi ngữ pháp hoặc ghi 'No major issues.')"",
    ""suggestion"": ""(câu gợi ý tự nhiên hơn)"",
    ""structureTip"": ""(mẹo nhỏ về ngữ pháp hoặc cấu trúc cần cải thiện)""
  }
}

Đầu vào người dùng:
Requirement: {{Requirement}}
Content: {{Content}}

Hãy phân tích và trả về đúng JSON trên.
";

    public static async Task<string> GenerateReview(string apiKey, EnglishLevel level, string requirement, string content)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("## **The writing requirement:**");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(requirement.Trim());
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## **The user’s writing submission:**");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(content.Trim());
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"## **The description of user’s current English proficiency level according to the CEFR:**");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(GeneralHelper.GetEnumDescription(level));
        promptBuilder.AppendLine();

        var generator = new Generator(apiKey);

        var apiRequest = new ApiRequestBuilder()
            .WithSystemInstruction(Instruction)
            .WithPrompt(promptBuilder.ToString())
            .WithDefaultGenerationConfig(0.5F, ResponseMimeType.PlainText)
            .DisableAllSafetySettings()
            .Build();

        var response = await generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash_Thinking);

        return response.Result;
    }
    public static async Task<string> GenerateDialogueAsync(
    string apiKey,
    string language,
    EnglishLevel level,
    string purpose,
    string topic)
    {
        var systemInstruction = @"
You are an AI English learning assistant for a writing and speaking practice platform.

Your task:
Generate a natural bilingual (English–Vietnamese) dialogue for the given topic.
Each line should represent a turn between two speakers (A and B or real names if provided).

The output must be **pure JSON only**, following this schema exactly:

{
  ""topic"": ""string"",
  ""purpose"": ""string"",
  ""difficulty"": ""string"",
  ""dialogue"": [
    {
      ""id"": number,
      ""speaker"": ""string"",
      ""vi"": ""string"",
      ""en"": ""string"",
      ""ai_suggestion"": {
        ""vocabulary"": [
          { ""word"": ""string"", ""meaning"": ""string"" },
          { ""word"": ""string"", ""meaning"": ""string"" }
        ],
        ""structure"": ""string""
      }
    }
  ]
}

Rules:
- Must include between 8 and 12 lines of dialogue.
- Keep sentences simple and natural, suitable for the user's English level.
- Vietnamese (`vi`) must be an accurate translation of English (`en`).
- Add `ai_suggestion` only for the first 3–4 lines.
- Keep `ai_suggestion.vocabulary` limited to 2–3 important words or phrases.
- `ai_suggestion.structure` explains the grammar or sentence form briefly (1–2 sentences in Vietnamese).
- Do NOT output any explanation, greeting, or markdown — JSON only.

Example output:
{
  ""topic"": ""Chào hỏi và làm quen"",
  ""purpose"": ""Giao tiếp"",
  ""difficulty"": ""Khá dễ"",
  ""dialogue"": [
    {
      ""id"": 1,
      ""speaker"": ""Mai"",
      ""vi"": ""Chào buổi sáng! Hình như đây là lần đầu mình thấy bạn ở đây."",
      ""en"": ""Good morning! I think this is the first time I've seen you here."",
      ""ai_suggestion"": {
        ""vocabulary"": [
          { ""word"": ""Good morning"", ""meaning"": ""Chào buổi sáng"" },
          { ""word"": ""the first time"", ""meaning"": ""lần đầu tiên"" }
        ],
        ""structure"": ""Câu chào hỏi đơn giản, sử dụng thì hiện tại hoàn thành: This is the first time + S + have/has + V3.""
      }
    },
    {
      ""id"": 2,
      ""speaker"": ""Nam"",
      ""vi"": ""Chào bạn, đúng vậy! Mình mới bắt đầu làm việc ở công ty này tuần trước."",
      ""en"": ""Hi, yes! I just started working at this company last week.""
    }
  ]
}
";


        var prompt = $"Language: {language}\nPurpose: {purpose}\nLevel: {level}\nTopic: {topic}";

        var generator = new Generator(apiKey);
        var apiRequest = new ApiRequestBuilder()
            .WithSystemInstruction(systemInstruction)
            .WithPrompt(prompt)
            .WithDefaultGenerationConfig(0.7F, ResponseMimeType.PlainText)
            .DisableAllSafetySettings()
            .Build();

        var response = await generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash_Thinking);
        return response.Result;
    }


}
