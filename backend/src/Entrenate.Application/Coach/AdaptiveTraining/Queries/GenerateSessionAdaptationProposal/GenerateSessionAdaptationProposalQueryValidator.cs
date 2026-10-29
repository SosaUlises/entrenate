using FluentValidation;

namespace Entrenate.Application.Coach.AdaptiveTraining.Queries.GenerateSessionAdaptationProposal
{
    public sealed class GenerateSessionAdaptationProposalQueryValidator
        : AbstractValidator<GenerateSessionAdaptationProposalQuery>
    {
        public GenerateSessionAdaptationProposalQueryValidator()
        {
            RuleFor(x => x.RoutineDayId)
                .NotEmpty()
                .WithMessage("El día de rutina es obligatorio.");

            When(x => x.AvailableMinutes.HasValue, () =>
            {
                RuleFor(x => x.AvailableMinutes!.Value)
                    .InclusiveBetween(15, 240)
                    .WithMessage("El tiempo disponible debe estar entre 15 y 240 minutos.");
            });
        }
    }
}

