using System.Net;
using System.Text;
using System.Text.Json;
using Entrenate.Application.Common.Models;
using Entrenate.Infrastructure.Coach.Conversation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Entrenate.Infrastructure.UnitTests.Coach.Conversation
{
    public class GroqCoachConversationProviderTests
    {
        [Fact]
        public async Task GenerateResponseAsync_Completed_ReturnsAnswer()
        {
            var handler = new QueueHttpMessageHandler(
                CompletedResponse("Respuesta breve."));
            var provider = CreateProvider(handler);

            var result = await provider.GenerateResponseAsync(CreatePrompt());

            Assert.Equal("Respuesta breve.", result.Answer);
            Assert.Single(handler.RequestBodies);
        }

        [Fact]
        public async Task GenerateResponseAsync_CompletedLongAnswer_ReturnsFullAnswer()
        {
            var expected = string.Join(
                " ",
                Enumerable.Repeat("Explicación fundamentada.", 80));
            var handler = new QueueHttpMessageHandler(
                CompletedResponse(expected));
            var provider = CreateProvider(handler);

            var result = await provider.GenerateResponseAsync(CreatePrompt());

            Assert.Equal(expected, result.Answer);
        }

        [Fact]
        public async Task GenerateResponseAsync_OutputLimit_RetriesWithDoubleBudget()
        {
            var handler = new QueueHttpMessageHandler(
                IncompleteResponse("Respuesta truncada."),
                CompletedResponse("Respuesta completa."));
            var provider = CreateProvider(handler);

            var result = await provider.GenerateResponseAsync(CreatePrompt());

            Assert.Equal("Respuesta completa.", result.Answer);
            Assert.Equal(2, handler.RequestBodies.Count);
            Assert.Equal(1200, ReadMaxOutputTokens(handler.RequestBodies[0]));
            Assert.Equal(2400, ReadMaxOutputTokens(handler.RequestBodies[1]));
        }

        [Fact]
        public async Task GenerateResponseAsync_RepeatedOutputLimit_RejectsPartialAnswer()
        {
            var handler = new QueueHttpMessageHandler(
                IncompleteResponse("Primera respuesta truncada."),
                IncompleteResponse("Segunda respuesta truncada."));
            var provider = CreateProvider(handler);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GenerateResponseAsync(CreatePrompt()));

            Assert.Contains("no completó", exception.Message);
            Assert.Equal(2, handler.RequestBodies.Count);
        }

        private static GroqCoachConversationProvider CreateProvider(
            HttpMessageHandler handler)
        {
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.groq.test/openai/v1/")
            };
            var settings = Options.Create(new GroqSettings
            {
                Model = "test-model",
                MaxOutputTokens = 1200
            });

            return new GroqCoachConversationProvider(
                client,
                settings,
                NullLogger<GroqCoachConversationProvider>.Instance);
        }

        private static CoachConversationPrompt CreatePrompt()
        {
            return new CoachConversationPrompt(
                "Respondé con datos reales.",
                "Explicá la propuesta.");
        }

        private static HttpResponseMessage CompletedResponse(string answer)
        {
            return JsonResponse(new
            {
                status = "completed",
                output = new[]
                {
                    new
                    {
                        type = "message",
                        content = new[]
                        {
                            new { type = "output_text", text = answer }
                        }
                    }
                }
            });
        }

        private static HttpResponseMessage IncompleteResponse(string partial)
        {
            return JsonResponse(new
            {
                status = "incomplete",
                incomplete_details = new { reason = "max_output_tokens" },
                output = new[]
                {
                    new
                    {
                        type = "message",
                        content = new[]
                        {
                            new { type = "output_text", text = partial }
                        }
                    }
                }
            });
        }

        private static HttpResponseMessage JsonResponse(object body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json")
            };
        }

        private static int ReadMaxOutputTokens(string requestBody)
        {
            using var document = JsonDocument.Parse(requestBody);
            return document.RootElement
                .GetProperty("max_output_tokens")
                .GetInt32();
        }

        private sealed class QueueHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;

            public QueueHttpMessageHandler(
                params HttpResponseMessage[] responses)
            {
                _responses = new Queue<HttpResponseMessage>(responses);
            }

            public List<string> RequestBodies { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestBodies.Add(
                    await request.Content!.ReadAsStringAsync(
                        cancellationToken));

                return _responses.Dequeue();
            }
        }
    }
}
