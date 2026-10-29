using FluentValidation;

namespace Entrenate.Application.Coach.AdaptiveTraining.Conversation.Commands.AskSessionAdaptationConversation
{
    public sealed class AskSessionAdaptationConversationCommandValidator
        : AbstractValidator<AskSessionAdaptationConversationCommand>
    {
        public AskSessionAdaptationConversationCommandValidator()
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

            RuleFor(x => x.Question)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("La pregunta es obligatoria.")
                .Must(q => !string.IsNullOrWhiteSpace(q))
                .WithMessage("La pregunta es obligatoria.")
                .MaximumLength(500)
                .WithMessage("La pregunta no puede superar los 500 caracteres.");
        }
    }
}

