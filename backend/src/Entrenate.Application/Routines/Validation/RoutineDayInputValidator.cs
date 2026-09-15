using Entrenate.Application.Routines.Models;
using FluentValidation;

namespace Entrenate.Application.Routines.Validation
{
    public class RoutineDayInputValidator
    : AbstractValidator<RoutineDayInput>
    {
        public RoutineDayInputValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty()
                .WithMessage("El nombre del día es obligatorio.")
                .MaximumLength(100)
                .WithMessage(
                    "El nombre del día no puede superar los 100 caracteres.");

            RuleFor(x => x.Descripcion)
                .MaximumLength(500)
                .WithMessage(
                    "La descripción del día no puede superar " +
                    "los 500 caracteres.");

            RuleFor(x => x.Orden)
                .GreaterThan(0)
                .WithMessage("El orden del día debe ser mayor que cero.");

            RuleFor(x => x.Ejercicios)
                .NotNull()
                .WithMessage("La lista de ejercicios es obligatoria.")
                .Must(ejercicios =>
                    ejercicios is null ||
                    ejercicios.Select(x => x.Orden).Distinct().Count() ==
                    ejercicios.Count)
                .WithMessage(
                    "No se puede repetir el orden de los ejercicios " +
                    "dentro de un día.");

            RuleForEach(x => x.Ejercicios)
                .SetValidator(new RoutineExerciseInputValidator());
        }
    }
}
