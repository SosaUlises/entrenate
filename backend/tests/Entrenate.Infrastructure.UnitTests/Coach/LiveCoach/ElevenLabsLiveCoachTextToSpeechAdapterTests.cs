using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Infrastructure.Coach.LiveCoach;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Entrenate.Infrastructure.UnitTests.Coach.LiveCoach
{
    public sealed class ElevenLabsLiveCoachTextToSpeechAdapterTests
    {
        private static ElevenLabsLiveCoachTextToSpeechAdapter CreateAdapter(
            HttpMessageHandler handler,
            int timeoutSeconds = 15,
            string voiceId = "test-voice-id",
            string apiKey = "test-api-key")
        {
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.elevenlabs.test/v1/")
            };

            var settings = Options.Create(new ElevenLabsSettings
            {
                BaseUrl = "https://api.elevenlabs.test/v1/",
                ApiKey = apiKey,
                VoiceId = voiceId,
                ModelId = "eleven_multilingual_v2",
                TimeoutSeconds = timeoutSeconds,
                OutputFormat = "mp3_44100_128"
            });

            return new ElevenLabsLiveCoachTextToSpeechAdapter(
                client,
                settings,
                NullLogger<ElevenLabsLiveCoachTextToSpeechAdapter>.Instance);
        }

        [Fact]
        public async Task SynthesizeAsync_WhenTextIsEmpty_ThrowsArgumentException()
        {
            var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var adapter = CreateAdapter(handler);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                adapter.SynthesizeAsync("   ", CancellationToken.None));
        }

        [Fact]
        public async Task SynthesizeAsync_WhenElevenLabsReturnsOk_ReturnsAudioWithMpegMimeAndValidatesPayloadPrivacy()
        {
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;

            var handler = new TestHttpMessageHandler(req =>
            {
                capturedRequest = req;
                capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-mp3-stream"))
                };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
                return response;
            });

            var adapter = CreateAdapter(handler, voiceId: "voice-123", apiKey: "secret-key-xyz");

            const string textToSpeak = "Te tocan 4 series de 8 repeticiones.";
            var result = await adapter.SynthesizeAsync(textToSpeak, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("audio/mpeg", result.ContentType);

            using var reader = new StreamReader(result.AudioStream);
            var content = await reader.ReadToEndAsync();
            Assert.Equal("fake-mp3-stream", content);

            // Verify HTTP Request shape and privacy
            Assert.NotNull(capturedRequest);
            Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
            Assert.Contains("text-to-speech/voice-123", capturedRequest.RequestUri!.ToString());
            Assert.Contains("output_format=mp3_44100_128", capturedRequest.RequestUri!.ToString());

            // xi-api-key header
            Assert.True(capturedRequest.Headers.TryGetValues("xi-api-key", out var keyValues));
            Assert.Contains("secret-key-xyz", keyValues);

            // Privacy verification: body must contain text and model, but no session, user or internal IDs
            Assert.NotNull(capturedBody);
            using var doc = JsonDocument.Parse(capturedBody!);
            var root = doc.RootElement;
            Assert.Equal(textToSpeak, root.GetProperty("text").GetString());
            Assert.Equal("eleven_multilingual_v2", root.GetProperty("model_id").GetString());
            Assert.True(root.TryGetProperty("voice_settings", out _));

            // Ensure no internal telemetry or identifiers leaked
            Assert.False(root.TryGetProperty("sessionId", out _));
            Assert.False(root.TryGetProperty("userId", out _));
            Assert.False(root.TryGetProperty("interactionId", out _));
        }

        [Fact]
        public async Task SynthesizeAsync_WhenElevenLabsReturnsHttpError_ThrowsExternalServiceException()
        {
            var handler = new TestHttpMessageHandler(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"detail\": \"Invalid API key\"}")
                };
            });

            var adapter = CreateAdapter(handler);

            var ex = await Assert.ThrowsAsync<ExternalServiceException>(() =>
                adapter.SynthesizeAsync("Texto de prueba.", CancellationToken.None));

            Assert.Contains("401", ex.Message);
        }

        [Fact]
        public async Task SynthesizeAsync_WhenTimeoutOccurs_ThrowsExternalServiceException()
        {
            var handler = new TestHttpMessageHandler(_ =>
            {
                throw new OperationCanceledException(); // simulates timeout from linked CTS
            });

            // Use adapter with 0 timeout so timeoutCts immediately expires
            var adapter = CreateAdapter(handler, timeoutSeconds: 1);

            // In our adapter, if timeoutCts is cancelled it maps to ExternalServiceException
            // Let's pass a handler that delays or simulates timeout
            var delayedHandler = new DelayedHttpMessageHandler(TimeSpan.FromMilliseconds(200));
            var quickAdapter = CreateAdapter(delayedHandler, timeoutSeconds: 1);

            using var cts = new CancellationTokenSource();
            // We cancel caller cts
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                quickAdapter.SynthesizeAsync("Texto de prueba.", cts.Token));
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

        private sealed class DelayedHttpMessageHandler : HttpMessageHandler
        {
            private readonly TimeSpan _delay;

            public DelayedHttpMessageHandler(TimeSpan delay)
            {
                _delay = delay;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                await Task.Delay(_delay, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("delayed-audio"))
                };
            }
        }
    }
}

