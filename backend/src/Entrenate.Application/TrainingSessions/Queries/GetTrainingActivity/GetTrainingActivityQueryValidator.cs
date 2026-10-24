using FluentValidation;

namespace Entrenate.Application.TrainingSessions.Queries.GetTrainingActivity
{
    public class GetTrainingActivityQueryValidator
        : AbstractValidator<GetTrainingActivityQuery>
    {
        private const int MaximumRangeDays = 366;

        public GetTrainingActivityQueryValidator()
        {
            RuleFor(x => x.From)
                .NotNull()
                .WithMessage("La fecha inicial es obligatoria.");

            RuleFor(x => x.To)
                .NotNull()
                .WithMessage("La fecha final es obligatoria.")
                .Must(to => to != DateOnly.MaxValue)
                .When(x => x.To.HasValue)
                .WithMessage("La fecha final está fuera del rango permitido.");

            RuleFor(x => x.To)
                .Must((query, to) =>
                    !query.From.HasValue ||
                    !to.HasValue ||
                    query.From.Value <= to.Value)
                .WithMessage("La fecha inicial debe ser anterior o igual a la fecha final.");

            RuleFor(x => x.To)
                .Must((query, to) =>
                    !query.From.HasValue ||
                    !to.HasValue ||
                    to.Value.DayNumber - query.From.Value.DayNumber + 1 <= MaximumRangeDays)
                .WithMessage($"El rango no puede superar {MaximumRangeDays} días.");

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
