using FluentValidation;

namespace Entrenate.Application.Auth.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator
     : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("La contraseña actual es obligatoria.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("La nueva contraseña es obligatoria.")
                .MinimumLength(8)
                .WithMessage("La nueva contraseña debe tener al menos 8 caracteres.")
                .Matches("[A-Z]")
                .WithMessage("La nueva contraseña debe contener al menos una letra mayúscula.")
                .Matches("[a-z]")
                .WithMessage("La nueva contraseña debe contener al menos una letra minúscula.")
                .Matches("[0-9]")
                .WithMessage("La nueva contraseña debe contener al menos un número.")
                .NotEqual(x => x.CurrentPassword)
                .WithMessage("La nueva contraseña debe ser diferente de la contraseña actual.");
        }
    }
}
