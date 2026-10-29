using System;
using Entrenate.Application.Coach.LiveCoach.Models;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.SynthesizeLiveSpeech
{
    public sealed record SynthesizeLiveSpeechCommand(Guid ResponseId) : IRequest<LiveCoachSpeechAudio>;
}

