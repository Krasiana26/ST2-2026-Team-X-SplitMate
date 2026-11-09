namespace SplitMate.Services;

public interface IAiAssistantService
{
    bool IsConfigured { get; }

    Task<string> GetReplyAsync(
        string message,
        string financialContext,
        CancellationToken cancellationToken);
}

public sealed class AiAssistantServiceException : Exception
{
    public AiAssistantServiceException()
        : base("The AI provider could not complete the request.")
    {
    }

    public AiAssistantServiceException(Exception innerException)
        : base("The AI provider could not complete the request.", innerException)
    {
    }
}
