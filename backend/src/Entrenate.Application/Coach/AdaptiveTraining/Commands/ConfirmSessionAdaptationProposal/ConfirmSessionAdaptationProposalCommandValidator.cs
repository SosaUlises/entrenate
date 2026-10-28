using FluentValidation;

namespace Entrenate.Application.Coach.AdaptiveTraining.Commands.ConfirmSessionAdaptationProposal
{
    public sealed class ConfirmSessionAdaptationProposalCommandValidator
        : AbstractValidator<ConfirmSessionAdaptationProposalCommand>
    {
        public ConfirmSessionAdaptationProposalCommandValidator()
        {
            RuleFor(x => x.ProposalId)
                .NotEmpty()
                .WithMessage("El identificador de propuesta es obligatorio.");

            RuleFor(x => x.ProposalHash)
                .NotEmpty()
                .WithMessage("El hash de la propuesta es obligatorio.");

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

