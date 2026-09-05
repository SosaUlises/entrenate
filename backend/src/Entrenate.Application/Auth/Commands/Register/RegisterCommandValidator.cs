using FluentValidation;

namespace Entrenate.Application.Auth.Commands.Register
{
    public class RegisterCommandValidator
    : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Nombre)
           .NotEmpty()
           .WithMessage("El nombre es obligatorio.")
           .MaximumLength(100)
           .WithMessage(
               "El nombre no puede superar los 100 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("El email es obligatorio.")
                .EmailAddress()
                .WithMessage("El email ingresado no es válido.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("La contraseña es obligatoria.")
                .MinimumLength(8)
                .WithMessage("La contraseña debe tener al menos 8 caracteres.")
                .Matches("[A-Z]")
                .WithMessage("La contraseña debe contener al menos una letra mayúscula.")
                .Matches("[a-z]")
                .WithMessage("La contraseña debe contener al menos una letra minúscula.")
                .Matches("[0-9]")
                .WithMessage("La contraseña debe contener al menos un número.");
        }
    }
}
