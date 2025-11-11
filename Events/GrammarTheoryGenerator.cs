using Gemini.NET;
using Models.Enums;
using System.Text;

namespace Events;

public class GrammarTheoryGenerator
{
    public const string Instruction = @"
Bạn là một trợ lý giảng dạy tiếng Anh, chuyên tạo ra các **bài học ngữ pháp có cấu trúc rõ ràng, dễ hiểu**.

Khi người dùng nhập vào **tiêu đề bài học (title_lesson)**, bạn cần **tạo ra phần lý thuyết tương ứng** với chủ đề đó.

Nội dung đầu ra **phải được trình bày đúng cấu trúc sau**:

Tiêu đề: [Ghi rõ tiêu đề bài học]

1. Định nghĩa:
   [Giải thích khái niệm một cách đơn giản, rõ ràng.]

2. Giải thích chi tiết:
   [Trình bày các quy tắc, phân loại hoặc cách sử dụng của chủ điểm ngữ pháp. 
    Có thể dùng gạch đầu dòng hoặc đoạn văn ngắn.]

3. Ví dụ minh họa:
   - [Ví dụ câu 1]
   - [Ví dụ câu 2]
   - [Ví dụ câu 3]

4. Tóm tắt kiến thức:
   [Liệt kê các điểm quan trọng người học cần ghi nhớ.]

Yêu cầu:
- Sử dụng ngôn ngữ đơn giản, phù hợp với người Việt học tiếng Anh.
- Ví dụ phải liên quan trực tiếp đến chủ đề.
- Hạn chế dùng thuật ngữ phức tạp, hoặc phải có giải thích đi kèm nếu bắt buộc sử dụng.
- Chỉ trả về **văn bản thuần túy** (plain text), **không JSON, không Markdown, không chào hỏi**.

Ví dụ đầu vào:
title_lesson = ""Bài 2 - Danh từ""

Kết quả mong đợi:

Tiêu đề: Bài 2 - Danh từ

1. Định nghĩa:
   Danh từ là từ dùng để gọi tên người, địa điểm, đồ vật hoặc khái niệm.

2. Giải thích chi tiết:
   - Danh từ chung: chỉ người/vật chung chung (ví dụ: học sinh, thành phố).
   - Danh từ riêng: chỉ tên riêng, viết hoa (ví dụ: Hà Nội, Lan).
   - Danh từ đếm được và không đếm được.
   - Vai trò của danh từ trong câu: làm chủ ngữ, tân ngữ...

3. Ví dụ minh họa:
   - Tôi thích âm nhạc.
   - Nam sống ở Hà Nội.
   - Cô ấy mua ba cuốn sách.

4. Tóm tắt kiến thức:
   - Danh từ là từ chỉ người, vật, nơi chốn, khái niệm.
   - Có nhiều loại: danh từ riêng, chung, đếm được/không đếm được.
   - Danh từ thường giữ vai trò quan trọng trong câu.
";

    public static async Task<string> GenerateTheoryAsync(
        string apiKey,
        string titleLesson
        )
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("## Thông tin đầu vào:");
        promptBuilder.AppendLine($"Tiêu đề bài học: {titleLesson.Trim()}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## Nhiệm vụ của bạn:");
        promptBuilder.AppendLine("Tạo phần lý thuyết ngữ pháp tiếng Anh theo đúng cấu trúc yêu cầu ở trên.");

        var generator = new Generator(apiKey);

        var apiRequest = new ApiRequestBuilder()
            .WithSystemInstruction(Instruction)
            .WithPrompt(promptBuilder.ToString())
            .WithDefaultGenerationConfig(0.6F, ResponseMimeType.PlainText)
            .DisableAllSafetySettings()
            .Build();

        var response = await generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash_Lite);

        return response.Result;
    }
}
