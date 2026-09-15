using FluentValidation;

namespace Entrenate.Application.TrainingSessions.Commands.UpsertTrainingSet
{
    public class UpsertTrainingSetCommandValidator
        : AbstractValidator<UpsertTrainingSetCommand>
    {
        public UpsertTrainingSetCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty()
                .WithMessage("La sesión de entrenamiento es obligatoria.");

            RuleFor(x => x.ExerciseSessionId)
                .NotEmpty()
                .WithMessage("El ejercicio de la sesión es obligatorio.");

            RuleFor(x => x.SetNumber)
                .GreaterThan(0)
                .WithMessage("El número de serie debe ser mayor que cero.");

            RuleFor(x => x.Peso)
                .InclusiveBetween(0, 1000)
                .WithMessage("El peso debe estar entre 0 y 1000 kg.");

            RuleFor(x => x.Repeticiones)
                .InclusiveBetween(1, 200)
                .WithMessage("Las repeticiones deben estar entre 1 y 200.");

            RuleFor(x => x.Rir)
                .Must(rir => !rir.HasValue ||
                    (rir.Value >= 0 && rir.Value <= 5))
                .WithMessage("El RIR debe estar entre 0 y 5.");
        }
    }
}
