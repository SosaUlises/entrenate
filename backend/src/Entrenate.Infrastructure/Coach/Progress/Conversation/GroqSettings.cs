namespace Entrenate.Infrastructure.Coach.Progress.Conversation
{
    public class GroqSettings
    {
        public string BaseUrl { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public int MaxOutputTokens { get; set; }

        public int TimeoutSeconds { get; set; }

        public string ApiKey { get; set; } = string.Empty;
    }
}
