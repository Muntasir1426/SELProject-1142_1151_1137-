namespace ggChick.Services
{
    public interface IGeminiService
    {
        Task<string> GenerateResponseAsync(string userPrompt, string? conversationContext = null);
    }
}
