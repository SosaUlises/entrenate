using Entrenate.Application.Routines.Validation;
using FluentValidation;

namespace Entrenate.Application.Routines.Commands.UpdateRoutine
{
    public class UpdateRoutineCommandValidator
    : AbstractValidator<UpdateRoutineCommand>
    {
        public UpdateRoutineCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("La rutina es obligatoria.");

            RuleFor(x => x.Nombre)
                .NotEmpty()
                .WithMessage("El nombre de la rutina es obligatorio.")
                .MaximumLength(100)
                .WithMessage(
                    "El nombre de la rutina no puede superar " +
                    "los 100 caracteres.");

            RuleFor(x => x.Descripcion)
                .MaximumLength(500)
                .WithMessage(
                    "La descripción de la rutina no puede superar " +
                    "los 500 caracteres.");

            RuleFor(x => x.Dias)
                .NotNull()
                .WithMessage("La lista de días es obligatoria.")
                .NotEmpty()
                .WithMessage("La rutina debe contener al menos un día.")
                .Must(dias =>
                    dias is null ||
                    dias.Select(x => x.Orden).Distinct().Count() ==
                    dias.Count)
                .WithMessage(
                    "No se puede repetir el orden de los días " +
                    "dentro de una rutina.");

            RuleForEach(x => x.Dias)
                .SetValidator(new RoutineDayInputValidator());
        }
    }
}
