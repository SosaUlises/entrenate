using FluentValidation;

namespace Entrenate.Application.Auth.Commands.Login
{
    public class LoginCommandValidator
     : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("El email es obligatorio.")
                .EmailAddress()
                .WithMessage("El email ingresado no es válido.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("La contraseña es obligatoria.");
        }
    }
}
