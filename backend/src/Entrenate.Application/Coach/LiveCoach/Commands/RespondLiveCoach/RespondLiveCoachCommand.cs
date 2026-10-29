using System;
using Entrenate.Application.Coach.LiveCoach.Context;
using Entrenate.Application.Coach.LiveCoach.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.RespondLiveCoach
{
    public sealed record RespondLiveCoachCommand(
        Guid SessionId,
        string ClientTurnId,
        string InputKind,
        string Text,
        ClientSnapshotInput? ClientSnapshot = null) : IRequest<LiveCoachRespondResponseDto>;
}

