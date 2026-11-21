using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.PostSession
{
    public sealed class AnswerPostSessionCoachQueryHandler
        : IRequestHandler<AnswerPostSessionCoachQuery, PostSessionCoachAnswerDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _repository;

        public AnswerPostSessionCoachQueryHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository repository)
        {
            _currentUserService = currentUserService;
            _repository = repository;
        }

        public async Task<PostSessionCoachAnswerDto> Handle(
            AnswerPostSessionCoachQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                throw new FluentValidation.ValidationException("La pregunta es obligatoria.");
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            var session = await _repository.GetByIdAndUserIdAsNoTrackingAsync(
                request.SessionId, userId, cancellationToken);
            if (session is null)
                throw new NotFoundException("La sesion de entrenamiento no existe.");
            PostSessionCoachSummaryDto summary;
            try { summary = PostSessionCoachSummaryEngine.Create(session); }
            catch (InvalidOperationException)
            {
                throw new ConflictException("La sesion todavia no esta completada.");
            }
            return new(PostSessionCoachAnswerResolver.Resolve(summary, request.Question));
        }
    }
}
