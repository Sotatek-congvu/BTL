using Entities.Enums;
using Gemini.NET;
using Models.Enums;
using System.Text;

namespace Events;

public class SpeakingTopic
{
    public const string Instruction_AnalyzeSpeech = @"
You are an experienced English speaking examiner (IELTS/TOEIC speaking evaluator).
Listen carefully to the learner's voice input and give a natural, plain-English evaluation.

Evaluation Criteria:
- Pronunciation (clarity, accuracy of sounds)
- Fluency (smoothness, rhythm, pauses)
- Intonation and stress (naturalness)
- Suggestion for improvement

Your task:
Analyze the uploaded audio and provide feedback in **plain text** only.
Do NOT output JSON, markdown, or symbols.

Response format example:
Transcript: (what the learner said)
Pronunciation score: 8 / 10
Fluency score: 7 / 10
Intonation score: 8 / 10
Feedback: ...
Suggestion: ...
";
    public const string Instruction_EvaluateAnswer_ByTopic = @"
You are an experienced English-speaking examiner and coach (IELTS/TOEIC level evaluator).

Your task:
Evaluate the learner's answer to a specific question within a given topic.

Context information:
- Topic: describes the general theme of the conversation or test section.
- Question: the exact question the learner is responding to.
- Learner's answer: the learner's spoken or written response.

Evaluation criteria (each 0–10):
1️⃣ **Content relevance** – Does the answer address the question meaningfully and appropriately for the topic?
2️⃣ **Grammar & structure** – Are tenses, syntax, and sentence patterns correct and natural?
3️⃣ **Vocabulary usage** – Is the vocabulary accurate, appropriate, and varied?
4️⃣ **Pronunciation** – Clarity, stress, and correctness of sounds (only if voice provided).
5️⃣ **Fluency & coherence** – Smoothness, pacing, and logical flow.
6️⃣ **Overall communication effectiveness** – How well does the learner convey their idea?

Your response must include:
- The topic and question again for context.
- The learner's transcript or text (if from voice, transcribe first).
- A short paragraph of feedback in natural English (no jargon).
- Suggestions for improvement.
- Then, individual scores for each category (0–10), and overall score (average).

🧾 **Output format (plain text only):**
Topic: ...
Question: ...
Learner's answer: ...
Transcript: ...
Feedback: ...
Suggestions: ...
Content: ... /10
Grammar: ... /10
Vocabulary: ... /10
Pronunciation: ... /10
Fluency: ... /10
Overall: ... /10
";


    public static async Task<string> AnalyzeSpeechAsync(
    string apiKey,
    string topic,
    string referenceSentence,
    byte[] audioBytes,
    string mimeType = "audio/wav")
    {
        var prompt = $@"
You are an English speaking examiner.
The learner is practicing a conversation under the topic: {topic}.

Here is the reference line the learner was supposed to say:
'{referenceSentence}'

1️⃣ Transcribe what the learner actually said.
2️⃣ Compare it directly with the reference sentence.
3️⃣ Score pronunciation, fluency, intonation, and accuracy (0–10 each).
4️⃣ If the learner said something completely different, clearly say: 'The learner did not pronounce the expected line.'
5️⃣ Give short, helpful feedback and suggestions for improvement.

Response format (plain text only):
Transcript: ...
Pronunciation score: ...
Fluency score: ...
Intonation score: ...
Accuracy score: ...
Feedback: ...
Suggestion: ...
";

        var requestBody = new
        {
            contents = new[]
            {
            new
            {
                parts = new object[]
                {
                    new { text = prompt },
                    new
                    {
                        inline_data = new
                        {
                            mime_type = mimeType,
                            data = Convert.ToBase64String(audioBytes)
                        }
                    }
                }
            }
        }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash-lite:generateContent?key={apiKey}";

        using var client = new HttpClient();
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var jsonString = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "(No response)";
        }
        catch
        {
            return jsonString;
        }
    }


    public static async Task<string> EvaluateAnswerAsync(
     string apiKey,
     string topic,
     string question,
     string learnerAnswer,
     byte[]? audioBytes = null,
     string mimeType = "audio/wav")
    {
        var prompt = $@"
Topic: {topic}
Question: {question}
Learner's answer: {learnerAnswer}
";

        var body = new
        {
            contents = new[]
            {
            new
            {
                parts = audioBytes == null
                    ? new object[] { new { text = Instruction_EvaluateAnswer_ByTopic + "\n\n" + prompt } }
                    : new object[]
                    {
                        new { text = Instruction_EvaluateAnswer_ByTopic + "\n\n" + prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = Convert.ToBase64String(audioBytes)
                            }
                        }
                    }
            }
        }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash-lite:generateContent?key={apiKey}";

        using var client = new HttpClient();
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var resString = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(resString);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "(No response)";
        }
        catch
        {
            return resString;
        }
    }




    public static async Task<string> GenerateDialogueAsync(
        string apiKey,
        string language,
        EnglishLevel level,
        string purpose,
        string topic)
    {
        var systemInstruction = @"
You are an AI English learning assistant for a speaking practice platform.
Your task is to generate a bilingual (English–Vietnamese) dialogue for the given topic.

The output must be **pure JSON only**, with this schema:
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
- 8–12 dialogue lines alternating between 2 speakers.
- Simple, natural sentences based on user's level.
- Vietnamese must be accurate translation of English.
- Add `ai_suggestion` only for first 3–4 lines.
- JSON only — no explanation, no markdown.
";

        var prompt = $@"
Language: {language}
Purpose: {purpose}
Level: {level}
Topic: {topic}
";

        var generator = new Generator(apiKey);
        var apiRequest = new ApiRequestBuilder()
            .WithSystemInstruction(systemInstruction)
            .WithPrompt(prompt)
            .WithDefaultGenerationConfig(0.7F, ResponseMimeType.PlainText)
            .DisableAllSafetySettings()
            .Build();

        var response = await generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash_Lite);
        return response.Result;
    }

}
