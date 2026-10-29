using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Commands.SynthesizeLiveSpeech;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Commands
{
    public sealed class SynthesizeLiveSpeechCommandHandlerTests
    {
        private readonly FakeLiveCoachResponseCache _responseCache;
        private readonly FakeLiveCoachTextToSpeech _textToSpeech;
        private readonly SynthesizeLiveSpeechCommandHandler _handler;

        public SynthesizeLiveSpeechCommandHandlerTests()
        {
            _responseCache = new FakeLiveCoachResponseCache();
            _textToSpeech = new FakeLiveCoachTextToSpeech();
            _handler = new SynthesizeLiveSpeechCommandHandler(
                _responseCache,
                _textToSpeech,
                NullLogger<SynthesizeLiveSpeechCommandHandler>.Instance);
        }

        [Fact]
        public async Task Handle_WhenResponseIdEmpty_ThrowsArgumentException()
        {
            var command = new SynthesizeLiveSpeechCommand(Guid.Empty);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenResponseIdNotFoundInCache_ThrowsNotFoundException()
        {
            var command = new SynthesizeLiveSpeechCommand(Guid.NewGuid());

            var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.Contains("no existe o ha expirado", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, _textToSpeech.CallCount);
        }

        [Fact]
        public async Task Handle_WhenResponseIdValid_SynthesizesExactApprovedTextAndReturnsAudio()
        {
            var responseId = Guid.NewGuid();
            const string approvedText = "Te tocan 3 series de 10 repeticiones.";
            _responseCache.Set(responseId, approvedText, TimeSpan.FromSeconds(60));

            var command = new SynthesizeLiveSpeechCommand(responseId);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("audio/mpeg", result.ContentType);
            Assert.Equal(1, _textToSpeech.CallCount);
            Assert.Equal(approvedText, _textToSpeech.LastText);
        }

        [Fact]
        public async Task Handle_WhenProviderThrowsException_ThrowsExternalServiceException()
        {
            var responseId = Guid.NewGuid();
            _responseCache.Set(responseId, "Texto válido.", TimeSpan.FromSeconds(60));
            _textToSpeech.ThrowOnCall = true;

            var command = new SynthesizeLiveSpeechCommand(responseId);

            var ex = await Assert.ThrowsAsync<ExternalServiceException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.Contains("servicio de síntesis de voz", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Handle_WhenCancelled_ThrowsOperationCanceledException()
        {
            var responseId = Guid.NewGuid();
            _responseCache.Set(responseId, "Texto válido.", TimeSpan.FromSeconds(60));
            _textToSpeech.SimulateCancellation = true;

            var command = new SynthesizeLiveSpeechCommand(responseId);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _handler.Handle(command, cts.Token));
        }

        private sealed class FakeLiveCoachResponseCache : ILiveCoachResponseCache
        {
            public Dictionary<Guid, string> Cache { get; } = [];

            public void Set(Guid responseId, string text, TimeSpan ttl)
            {
                Cache[responseId] = text;
            }

            public bool TryGet(Guid responseId, out string? text)
            {
                return Cache.TryGetValue(responseId, out text);
            }
        }

        private sealed class FakeLiveCoachTextToSpeech : ILiveCoachTextToSpeech
        {
            public int CallCount { get; private set; }
            public string? LastText { get; private set; }
            public bool ThrowOnCall { get; set; }
            public bool SimulateCancellation { get; set; }

            public Task<LiveCoachSpeechAudio> SynthesizeAsync(
                string text,
                CancellationToken cancellationToken = default)
            {
                if (SimulateCancellation || cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                if (ThrowOnCall)
                {
                    throw new InvalidOperationException("ElevenLabs connection failure.");
                }

                CallCount++;
                LastText = text;

                var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-audio-bytes"));
                return Task.FromResult(new LiveCoachSpeechAudio(stream, "audio/mpeg"));
            }
        }
    }
}

