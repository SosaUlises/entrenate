using FluentValidation;

namespace Entrenate.Application.Coach.RoutinePlanner.Conversation.Commands.AskRoutinePlannerConversation
{
    public sealed class AskRoutinePlannerConversationCommandValidator
        : AbstractValidator<AskRoutinePlannerConversationCommand>
    {
        public AskRoutinePlannerConversationCommandValidator()
        {
            RuleFor(command => command.Question)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("La pregunta es obligatoria.")
                .Must(question => !string.IsNullOrWhiteSpace(question))
                .WithMessage("La pregunta es obligatoria.")
                .MaximumLength(500)
                .WithMessage(
                    "La pregunta no puede superar los 500 caracteres.");
        }
    }
}
