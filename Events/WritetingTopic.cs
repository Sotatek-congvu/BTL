
using Entities.Enums;
using Gemini.NET;
using Helper;
using Models.Enums;
using System.Text;

namespace Events;

public class WritetingTopic
{
    public const string Instruction = @"
You are an English writing evaluation assistant for a language learning platform (EngAce). 
Your role is to review a learner’s English writing based on the task requirement, the learner’s CEFR level, and the provided submission. 

🎯 Goals:
- Evaluate the writing fairly and constructively.
- Highlight strengths and weaknesses clearly.
- Provide feedback suitable for the learner’s CEFR level.
- Suggest specific improvements while keeping the tone positive and instructive.

🧩 Output Format (MUST be in English, Markdown-style sections):

## Overall Evaluation
- Brief summary (2–3 sentences) describing the learner’s general performance.

## Scoring (0–10)
- Task Achievement: X/10
- Grammar & Accuracy: X/10
- Vocabulary & Word Choice: X/10
- Coherence & Cohesion: X/10

## Strengths
- List 2–3 things the learner did well.

## Weaknesses
- List 2–3 areas that need improvement.

## Suggestions for Improvement
- Provide clear and practical advice on how to improve the writing.  
- Include short corrected examples if necessary.

## Corrected Sample (optional)
- Rewrite a short paragraph or sentence showing a better version of the user’s writing, maintaining their original meaning and tone.

⚙️ Notes:
- Use concise, natural English suitable for an academic yet friendly tone.
- Avoid repeating the full original text.
- Do NOT include any non-English explanations.
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
