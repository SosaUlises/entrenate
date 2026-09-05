using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Entrenate.Application.TrainingProfiles.Commands.CreateTrainingProfile
{
    public class CreateTrainingProfileCommandHandler
    : IRequestHandler<CreateTrainingProfileCommand, Guid>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingProfileRepository _repository;

        public CreateTrainingProfileCommandHandler(
            ICurrentUserService currentUserService,
            ITrainingProfileRepository repository)
        {
            _currentUserService = currentUserService;
            _repository = repository;
        }

        public async Task<Guid> Handle(
            CreateTrainingProfileCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var existingProfile =
                await _repository.GetByUserIdAsync(
                    userId,
                    cancellationToken);

            if (existingProfile is not null)
            {
                throw new ConflictException(
                    "El usuario ya tiene un perfil de entrenamiento.");
            }

            var requestedEquipmentIds =
                request.EquipamientoIds
                    .Distinct()
                    .ToList();

            if (requestedEquipmentIds.Count > 0)
            {
                var validEquipmentIds =
                    await _repository.GetValidEquipmentIdsAsync(
                        requestedEquipmentIds,
                        cancellationToken);

                if (validEquipmentIds.Count !=
                    requestedEquipmentIds.Count)
                {
                    throw new ValidationException(
                        new[]
                        {
                        new ValidationFailure(
                            nameof(request.EquipamientoIds),
                            "Uno o más equipamientos seleccionados " +
                            "no existen o no están disponibles.")
                        });
                }
            }

            var perfil = new PerfilEntrenamiento(
                userId,
                request.Objetivo,
                request.NivelExperiencia,
                request.Edad,
                request.Sexo,
                request.PesoKg,
                request.DiasEntrenamientoPorSemana,
                request.DuracionSesionMinutos,
                request.EntornoEntrenamiento);

            perfil.DefinirDiasPreferidos(
                request.DiasPreferidos);

            perfil.DefinirEquipamientos(
                requestedEquipmentIds);

            await _repository.AddAsync(
                perfil,
                cancellationToken);

            await _repository.SaveChangesAsync(
                cancellationToken);

            return perfil.Id;
        }
    }
}
