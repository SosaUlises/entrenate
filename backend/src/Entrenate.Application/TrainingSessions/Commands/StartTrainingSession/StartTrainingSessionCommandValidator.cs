using FluentValidation;

namespace Entrenate.Application.TrainingSessions.Commands.StartTrainingSession
{
    public class StartTrainingSessionCommandValidator
        : AbstractValidator<StartTrainingSessionCommand>
    {
        public StartTrainingSessionCommandValidator()
        {
            RuleFor(x => x.DiaRutinaId)
                .NotEmpty()
                .WithMessage("El día de rutina es obligatorio.");
        }
    }
}
