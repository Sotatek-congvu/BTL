using Entities.Enums;

namespace EngAce.Api.DTO;

public class WritingTopicRequest
{
    public string Language { get; set; } = "English";
    public EnglishLevel Level { get; set; } = EnglishLevel.Intermediate;
    public string Purpose { get; set; } = "Giao tiếp";
    public string Topic { get; set; } = string.Empty;
    public string CreationMode { get; set; } = "AI_tao_tu_chu_de";
}
