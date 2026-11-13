using System;
using System.IO;
using Entrenate.Application.Coach.LiveCoach.Models;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.TranscribeLiveAudio
{
    public sealed record TranscribeLiveAudioCommand(
        Stream AudioStream,
        string MediaType,
        long ByteLength,
        Guid InteractionId,
        string ClientTurnId,
        int? DurationMilliseconds = null) : IRequest<LiveCoachTranscriptResult>;
}

