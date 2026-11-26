using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.PostSession
{
    public sealed class GetPostSessionCoachSummaryQueryHandler
        : IRequestHandler<GetPostSessionCoachSummaryQuery, PostSessionCoachSummaryDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _repository;

        public GetPostSessionCoachSummaryQueryHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository repository)
        {
            _currentUserService = currentUserService;
            _repository = repository;
        }

        public async Task<PostSessionCoachSummaryDto> Handle(
            GetPostSessionCoachSummaryQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            var session = await _repository.GetByIdAndUserIdAsNoTrackingAsync(
                request.SessionId, userId, cancellationToken);
            if (session is null)
                throw new NotFoundException("La sesion de entrenamiento no existe.");
            try
            {
                return PostSessionCoachSummaryEngine.Create(session);
            }
            catch (InvalidOperationException)
            {
                throw new ConflictException("La sesion todavia no esta completada.");
            }
        }
    }
}
