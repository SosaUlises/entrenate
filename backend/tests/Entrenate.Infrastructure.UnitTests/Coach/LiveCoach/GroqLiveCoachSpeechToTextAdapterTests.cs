using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Infrastructure.Coach.Conversation;
using Entrenate.Infrastructure.Coach.LiveCoach;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Entrenate.Infrastructure.UnitTests.Coach.LiveCoach
{
    public sealed class GroqLiveCoachSpeechToTextAdapterTests
    {
        private static GroqLiveCoachSpeechToTextAdapter CreateAdapter(HttpMessageHandler handler, int timeoutSeconds = 10)
        {
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.groq.test/openai/v1/")
            };

            var settings = Options.Create(new GroqSettings
            {
                BaseUrl = "https://api.groq.test/openai/v1/",
                ApiKey = "gsk-test-key",
                SttModel = "whisper-large-v3-turbo",
                SttTimeoutSeconds = timeoutSeconds
            });

            return new GroqLiveCoachSpeechToTextAdapter(
                client,
                settings,
                NullLogger<GroqLiveCoachSpeechToTextAdapter>.Instance);
        }

        [Fact]
        public async Task TranscribeAsync_WhenGroqReturnsOk_ReturnsSuccessWithTranscript()
        {
            var handler = new TestHttpMessageHandler(req =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new
                        {
                            text = "  ¿Cuántas repeticiones me tocan?  ",
                            language = "es"
                        }),
                        Encoding.UTF8,
                        "application/json")
                };
            });

            var adapter = CreateAdapter(handler);
            using var stream = new MemoryStream(new byte[2048]);
            var input = new LiveCoachAudioInput(stream, "audio/webm;codecs=opus", 2048);

            var result = await adapter.TranscribeAsync(input, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.Success, result.Status);
            Assert.Equal("¿Cuántas repeticiones me tocan?", result.Transcript);
            Assert.Equal("es", result.DetectedLocale);
        }

        [Fact]
        public async Task TranscribeAsync_WhenGroqReturnsEmptyText_ReturnsEmptyTranscript()
        {
            var handler = new TestHttpMessageHandler(req =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { text = "   " }),
                        Encoding.UTF8,
                        "application/json")
                };
            });

            var adapter = CreateAdapter(handler);
            using var stream = new MemoryStream(new byte[2048]);
            var input = new LiveCoachAudioInput(stream, "audio/webm", 2048);

            var result = await adapter.TranscribeAsync(input, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.EmptyTranscript, result.Status);
            Assert.Empty(result.Transcript);
        }

        [Fact]
        public async Task TranscribeAsync_WhenGroqReturnsBadRequest_ReturnsUnsupportedAudio()
        {
            var handler = new TestHttpMessageHandler(req =>
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Invalid file format")
                };
            });

            var adapter = CreateAdapter(handler);
            using var stream = new MemoryStream(new byte[2048]);
            var input = new LiveCoachAudioInput(stream, "audio/unknown", 2048);

            var result = await adapter.TranscribeAsync(input, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.UnsupportedAudio, result.Status);
        }

        [Fact]
        public async Task TranscribeAsync_WhenGroqReturnsServerError_ReturnsProviderUnavailable()
        {
            var handler = new TestHttpMessageHandler(req =>
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            });

            var adapter = CreateAdapter(handler);
            using var stream = new MemoryStream(new byte[2048]);
            var input = new LiveCoachAudioInput(stream, "audio/webm", 2048);

            var result = await adapter.TranscribeAsync(input, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.ProviderUnavailable, result.Status);
        }

        [Fact]
        public async Task TranscribeAsync_WhenConnectionFails_ReturnsProviderUnavailable()
        {
            var handler = new TestHttpMessageHandler(req =>
            {
                throw new HttpRequestException("Network down");
            });

            var adapter = CreateAdapter(handler);
            using var stream = new MemoryStream(new byte[2048]);
            var input = new LiveCoachAudioInput(stream, "audio/webm", 2048);

            var result = await adapter.TranscribeAsync(input, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.ProviderUnavailable, result.Status);
        }

        [Fact]
        public async Task TranscribeAsync_WhenTimeoutOccurs_ReturnsTimeout()
        {
            var handler = new TestHttpMessageHandler(req =>
            {
                throw new OperationCanceledException();
            });

            var adapter = CreateAdapter(handler);
            using var stream = new MemoryStream(new byte[2048]);
            var input = new LiveCoachAudioInput(stream, "audio/webm", 2048);

            var result = await adapter.TranscribeAsync(input, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.Timeout, result.Status);
        }

        private sealed class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_responder(request));
            }
        }
    }
}

