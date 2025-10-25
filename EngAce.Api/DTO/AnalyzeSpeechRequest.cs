
namespace EngAce.Api.DTO;


public class AnalyzeSpeechRequest
{
    public string Topic { get; set; }
    public string Reference { get; set; }
    public IFormFile File { get; set; } = default;
}

public class EvaluateAnswerRequest
{
    public string Topic { get; set; }
    public string Question { get; set; }
    public string? AnswerText { get; set; }
    public IFormFile? File { get; set; }
}

public class NextDialogueRequest
{
    public string DialogueJson { get; set; }
    public int CurrentId { get; set; }
}