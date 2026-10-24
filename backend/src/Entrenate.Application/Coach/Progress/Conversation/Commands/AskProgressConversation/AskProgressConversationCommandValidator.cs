using FluentValidation;

namespace Entrenate.Application.Coach.Progress.Conversation.Commands.AskProgressConversation
{
    public class AskProgressConversationCommandValidator
        : AbstractValidator<AskProgressConversationCommand>
    {
        public AskProgressConversationCommandValidator()
        {
            RuleFor(command => command.Question)
                .NotEmpty()
                .WithMessage("La pregunta es obligatoria.")
                .MaximumLength(500)
                .WithMessage("La pregunta no puede superar los 500 caracteres.");
        }
    }
}
