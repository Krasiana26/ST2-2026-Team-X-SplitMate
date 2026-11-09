using System.ComponentModel.DataAnnotations;

namespace SplitMate.Models;

public sealed class AiAssistantRequest
{
    public const int MaximumMessageLength = 2000;

    [Required]
    [StringLength(MaximumMessageLength)]
    public string Message { get; set; } = string.Empty;
}
