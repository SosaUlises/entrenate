using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingProfiles.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingProfiles.Queries.GetMyTrainingProfile
{
    public class GetMyTrainingProfileQueryHandler
      : IRequestHandler<
          GetMyTrainingProfileQuery,
          TrainingProfileDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingProfileRepository _trainingProfileRepository;
        private readonly IEquipmentRepository _equipmentRepository;

        public GetMyTrainingProfileQueryHandler(
            ICurrentUserService currentUserService,
            ITrainingProfileRepository trainingProfileRepository,
            IEquipmentRepository equipmentRepository)
        {
            _currentUserService = currentUserService;
            _trainingProfileRepository = trainingProfileRepository;
            _equipmentRepository = equipmentRepository;
        }

        public async Task<TrainingProfileDto> Handle(
            GetMyTrainingProfileQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var perfil =
                await _trainingProfileRepository.GetByUserIdAsync(
                    userId,
                    cancellationToken);

            if (perfil is null)
            {
                throw new NotFoundException(
                    "El usuario todavía no tiene un perfil de entrenamiento.");
            }

            var equipmentIds = perfil
                .Equipamientos
                .Select(x => x.EquipamientoId)
                .ToList();

            var equipment =
                await _equipmentRepository.GetByIdsAsync(
                    equipmentIds,
                    cancellationToken);

            var equipmentDtos = equipment
                .Select(x =>
                    new TrainingProfileEquipmentDto(
                        x.Id,
                        x.Nombre,
                        x.Categoria))
                .ToList();

            var preferredDays = perfil
                .DiasPreferidos
                .Select(x => x.Dia)
                .OrderBy(x => x)
                .ToList();

            return new TrainingProfileDto(
                perfil.Id,
                perfil.Objetivo,
                perfil.NivelExperiencia,
                perfil.Edad,
                perfil.Sexo,
                perfil.PesoKg,
                perfil.DiasEntrenamientoPorSemana,
                perfil.DuracionSesionMinutos,
                perfil.EntornoEntrenamiento,
                preferredDays,
                equipmentDtos,
                perfil.CreadoEnUtc,
                perfil.ActualizadoEnUtc);
        }
    }
}
