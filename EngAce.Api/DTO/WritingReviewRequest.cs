using Entities.Enums;

namespace EngAce.Api.DTO;

public class WritingReviewRequest
{
    /// <summary>
    /// Trình độ tiếng Anh hiện tại của người học (theo CEFR).
    /// </summary>
    public EnglishLevel Level { get; set; }

    /// <summary>
    /// Yêu cầu đề bài (ví dụ: Write about your favorite place...)
    /// </summary>
    public string Requirement { get; set; } = string.Empty;

    /// <summary>
    /// Nội dung bài viết người học nộp.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
