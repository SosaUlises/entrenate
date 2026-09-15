using Entrenate.Application.Routines.Models;
using FluentValidation;

namespace Entrenate.Application.Routines.Validation
{
    public class RoutineExerciseInputValidator
    : AbstractValidator<RoutineExerciseInput>
    {
        public RoutineExerciseInputValidator()
        {
            RuleFor(x => x.EjercicioId)
                .NotEmpty()
                .WithMessage("El ejercicio es obligatorio.");

            RuleFor(x => x.CantidadSeries)
                .InclusiveBetween(1, 20)
                .WithMessage(
                    "La cantidad de series debe estar entre 1 y 20.");

            RuleFor(x => x.RepeticionesMinimas)
                .InclusiveBetween(1, 100)
                .WithMessage(
                    "Las repeticiones mínimas deben estar entre 1 y 100.");

            RuleFor(x => x.RepeticionesMaximas)
                .InclusiveBetween(1, 100)
                .WithMessage(
                    "Las repeticiones máximas deben estar entre 1 y 100.");

            RuleFor(x => x)
                .Must(x =>
                    x.RepeticionesMinimas <= x.RepeticionesMaximas)
                .WithMessage(
                    "Las repeticiones mínimas no pueden ser mayores " +
                    "que las máximas.");

            RuleFor(x => x.RirObjetivoMinimo)
                .InclusiveBetween(0, 5)
                .WithMessage("El RIR mínimo debe estar entre 0 y 5.");

            RuleFor(x => x.RirObjetivoMaximo)
                .InclusiveBetween(0, 5)
                .WithMessage("El RIR máximo debe estar entre 0 y 5.");

            RuleFor(x => x)
                .Must(x =>
                    x.RirObjetivoMinimo <= x.RirObjetivoMaximo)
                .WithMessage(
                    "El RIR mínimo no puede ser mayor que el máximo.");

            RuleFor(x => x.DescansoSegundos)
                .InclusiveBetween(15, 900)
                .WithMessage(
                    "El descanso debe estar entre 15 y 900 segundos.");

            RuleFor(x => x.Orden)
                .GreaterThan(0)
                .WithMessage("El orden del ejercicio debe ser mayor que cero.");

            RuleFor(x => x.Notas)
                .MaximumLength(500)
                .WithMessage(
                    "Las notas no pueden superar los 500 caracteres.");
        }
    }
}
