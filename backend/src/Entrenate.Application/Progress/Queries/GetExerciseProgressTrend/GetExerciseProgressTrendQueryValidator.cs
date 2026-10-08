using FluentValidation;

namespace Entrenate.Application.Progress.Queries.GetExerciseProgressTrend
{
    public class GetExerciseProgressTrendQueryValidator
        : AbstractValidator<GetExerciseProgressTrendQuery>
    {
        public GetExerciseProgressTrendQueryValidator()
        {
            RuleFor(x => x)
                .Must(x => x.From.HasValue == x.To.HasValue)
                .WithMessage("Las fechas inicial y final deben enviarse juntas.");

            RuleFor(x => x.To)
                .Must(to => to != DateOnly.MaxValue)
                .When(x => x.To.HasValue)
                .WithMessage("La fecha final está fuera del rango permitido.");

            RuleFor(x => x.To)
                .Must((query, to) =>
                    !query.From.HasValue ||
                    !to.HasValue ||
                    query.From.Value <= to.Value)
                .WithMessage("La fecha inicial debe ser anterior o igual a la fecha final.");

            RuleFor(x => x.TimeZone)
                .NotEmpty()
                .WithMessage("La zona horaria es obligatoria.")
                .Must(BeValidTimeZone)
                .When(x => !string.IsNullOrWhiteSpace(x.TimeZone))
                .WithMessage("La zona horaria no es válida.");
        }

        private static bool BeValidTimeZone(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                return false;
            }

            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return true;
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
            catch (InvalidTimeZoneException)
            {
                return false;
            }
        }
    }
}
