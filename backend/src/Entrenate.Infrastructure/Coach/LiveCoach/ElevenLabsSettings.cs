namespace Entrenate.Infrastructure.Coach.LiveCoach
{
    public class ElevenLabsSettings
    {
        public string BaseUrl { get; set; } = "https://api.elevenlabs.io/v1/";

        public string ApiKey { get; set; } = string.Empty;

        public string VoiceId { get; set; } = "21m00Tcm4TlvDq8ikWAM";

        public string ModelId { get; set; } = "eleven_multilingual_v2";

        public int TimeoutSeconds { get; set; } = 15;

        public string OutputFormat { get; set; } = "mp3_44100_128";
    }
}

