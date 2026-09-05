using Entrenate.Application.TrainingProfiles.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingProfiles.Queries.GetMyTrainingProfile
{
    public record GetMyTrainingProfileQuery
      : IRequest<TrainingProfileDto>;
}
