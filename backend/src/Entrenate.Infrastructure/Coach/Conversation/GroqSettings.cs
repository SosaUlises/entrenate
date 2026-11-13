namespace Entrenate.Infrastructure.Coach.Conversation
{
    public class GroqSettings
    {
        public string BaseUrl { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string SttModel { get; set; } = "whisper-large-v3-turbo";

        public int MaxOutputTokens { get; set; }

        public int TimeoutSeconds { get; set; }

        public int SttTimeoutSeconds { get; set; } = 10;

        public string ApiKey { get; set; } = string.Empty;
    }
}
