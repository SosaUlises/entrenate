using FluentValidation;

namespace Entrenate.Application.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandValidator
     : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("El email es obligatorio.")
                .EmailAddress()
                .WithMessage("El email ingresado no es válido.");
        }
    }
}
