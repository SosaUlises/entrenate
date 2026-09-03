using FluentValidation;

namespace Entrenate.Application.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator
     : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("El email es obligatorio.")
                .EmailAddress()
                .WithMessage("El email ingresado no es válido.");

            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("El token de recuperación es obligatorio.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("La nueva contraseña es obligatoria.")
                .MinimumLength(8)
                .WithMessage(
                    "La nueva contraseña debe tener al menos 8 caracteres.")
                .Matches("[A-Z]")
                .WithMessage(
                    "La nueva contraseña debe contener al menos una letra mayúscula.")
                .Matches("[a-z]")
                .WithMessage(
                    "La nueva contraseña debe contener al menos una letra minúscula.")
                .Matches("[0-9]")
                .WithMessage(
                    "La nueva contraseña debe contener al menos un número.");
        }
    }
}
