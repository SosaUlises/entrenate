using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionHistory
{
    public class GetTrainingSessionHistoryQueryHandler
        : IRequestHandler<
            GetTrainingSessionHistoryQuery,
            IReadOnlyCollection<TrainingSessionHistorySummaryDto>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _sessionRepository;

        public GetTrainingSessionHistoryQueryHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _sessionRepository = sessionRepository;
        }

        public async Task<IReadOnlyCollection<TrainingSessionHistorySummaryDto>>
            Handle(
                GetTrainingSessionHistoryQuery request,
                CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            return await _sessionRepository.GetHistoryByUserIdAsync(
                userId,
                cancellationToken);
        }
    }
}
