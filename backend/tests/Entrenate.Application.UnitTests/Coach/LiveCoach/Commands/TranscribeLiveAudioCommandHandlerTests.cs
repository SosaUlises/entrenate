using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Commands.TranscribeLiveAudio;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Application.Common.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Commands
{
    public sealed class TranscribeLiveAudioCommandHandlerTests
    {
        private readonly FakeLiveCoachSpeechToText _fakeSpeechToText;
        private readonly TranscribeLiveAudioCommandHandler _handler;

        public TranscribeLiveAudioCommandHandlerTests()
        {
            _fakeSpeechToText = new FakeLiveCoachSpeechToText();
            _handler = new TranscribeLiveAudioCommandHandler(
                _fakeSpeechToText,
                NullLogger<TranscribeLiveAudioCommandHandler>.Instance);
        }

        [Fact]
        public async Task Handle_WhenAudioBelowMinimumSize_ReturnsEmptyAudio()
        {
            using var stream = new MemoryStream(new byte[500]);
            var command = new TranscribeLiveAudioCommand(
                stream,
                "audio/webm;codecs=opus",
                500,
                Guid.NewGuid(),
                "turn-1");

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.EmptyAudio, result.Status);
            Assert.Equal(0, _fakeSpeechToText.CallCount);
        }

        [Fact]
        public async Task Handle_WhenAudioExceedsMaxSize_ReturnsAudioTooLarge()
        {
            var largeSize = (2 * 1024 * 1024) + 1;
            using var stream = new MemoryStream();
            var command = new TranscribeLiveAudioCommand(
                stream,
                "audio/webm;codecs=opus",
                largeSize,
                Guid.NewGuid(),
                "turn-1");

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.AudioTooLarge, result.Status);
            Assert.Equal(0, _fakeSpeechToText.CallCount);
        }

        [Fact]
        public async Task Handle_WhenDurationExceedsMax_ReturnsAudioTooLong()
        {
            using var stream = new MemoryStream(new byte[2000]);
            var command = new TranscribeLiveAudioCommand(
                stream,
                "audio/webm;codecs=opus",
                2000,
                Guid.NewGuid(),
                "turn-1",
                DurationMilliseconds: 18500);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.AudioTooLong, result.Status);
            Assert.Equal(0, _fakeSpeechToText.CallCount);
        }

        [Theory]
        [InlineData("text/plain")]
        [InlineData("video/mp4")]
        [InlineData("application/octet-stream")]
        [InlineData("")]
        public async Task Handle_WhenMimeTypeNotAllowed_ReturnsUnsupportedAudio(string mimeType)
        {
            using var stream = new MemoryStream(new byte[2000]);
            var command = new TranscribeLiveAudioCommand(
                stream,
                mimeType,
                2000,
                Guid.NewGuid(),
                "turn-1");

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.UnsupportedAudio, result.Status);
            Assert.Equal(0, _fakeSpeechToText.CallCount);
        }

        [Theory]
        [InlineData("audio/webm;codecs=opus")]
        [InlineData("audio/mp4")]
        [InlineData("audio/ogg;codecs=opus")]
        [InlineData("audio/wav")]
        [InlineData("audio/mpeg")]
        public async Task Handle_WhenValidAudio_CallsProviderAndReturnsSuccess(string mimeType)
        {
            using var stream = new MemoryStream(new byte[5000]);
            var command = new TranscribeLiveAudioCommand(
                stream,
                mimeType,
                5000,
                Guid.NewGuid(),
                "turn-1",
                DurationMilliseconds: 4500);

            _fakeSpeechToText.ResultToReturn = new LiveCoachTranscriptResult(
                "¿Cuántas repeticiones me tocan?",
                0.95m,
                "es",
                true,
                LiveCoachTranscriptStatus.Success);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.Success, result.Status);
            Assert.Equal("¿Cuántas repeticiones me tocan?", result.Transcript);
            Assert.Equal(1, _fakeSpeechToText.CallCount);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("...")]
        [InlineData("¿?")]
        [InlineData(" . , - ")]
        public async Task Handle_WhenProviderReturnsPunctuationOrWhitespace_ReturnsEmptyTranscript(string transcript)
        {
            using var stream = new MemoryStream(new byte[5000]);
            var command = new TranscribeLiveAudioCommand(
                stream,
                "audio/webm",
                5000,
                Guid.NewGuid(),
                "turn-1");

            _fakeSpeechToText.ResultToReturn = new LiveCoachTranscriptResult(
                transcript,
                null,
                "es",
                true,
                LiveCoachTranscriptStatus.Success);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(LiveCoachTranscriptStatus.EmptyTranscript, result.Status);
            Assert.Empty(result.Transcript);
            Assert.Equal(1, _fakeSpeechToText.CallCount);
        }

        private sealed class FakeLiveCoachSpeechToText : ILiveCoachSpeechToText
        {
            public int CallCount { get; private set; }
            public LiveCoachTranscriptResult ResultToReturn { get; set; } = new(
                "Ok",
                null,
                "es",
                true,
                LiveCoachTranscriptStatus.Success);

            public Task<LiveCoachTranscriptResult> TranscribeAsync(
                LiveCoachAudioInput input,
                CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(ResultToReturn);
            }
        }
    }
}
