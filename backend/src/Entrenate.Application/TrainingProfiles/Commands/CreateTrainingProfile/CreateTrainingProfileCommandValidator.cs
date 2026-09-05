using FluentValidation;

namespace Entrenate.Application.TrainingProfiles.Commands.CreateTrainingProfile
{
    public class CreateTrainingProfileCommandValidator
     : AbstractValidator<CreateTrainingProfileCommand>
    {
        public CreateTrainingProfileCommandValidator()
        {
            RuleFor(x => x.Objetivo)
                .IsInEnum()
                .WithMessage(
                    "El objetivo de entrenamiento no es válido.");

            RuleFor(x => x.NivelExperiencia)
                .IsInEnum()
                .WithMessage(
                    "El nivel de experiencia no es válido.");

            RuleFor(x => x.Edad)
                .GreaterThan(0)
                .WithMessage(
                    "La edad debe ser mayor a cero.")
                .LessThanOrEqualTo(120)
                .WithMessage(
                    "La edad ingresada no es válida.");

            RuleFor(x => x.Sexo)
                .Must(sexo =>
                    sexo is null ||
                    Enum.IsDefined(sexo.Value))
                .WithMessage(
                    "El sexo ingresado no es válido.");

            RuleFor(x => x.PesoKg)
                .GreaterThan(0)
                .When(x => x.PesoKg.HasValue)
                .WithMessage(
                    "El peso debe ser mayor a cero.");

            RuleFor(x => x.DiasEntrenamientoPorSemana)
                .InclusiveBetween(1, 7)
                .WithMessage(
                    "Los días de entrenamiento deben estar entre 1 y 7.");

            RuleFor(x => x.DuracionSesionMinutos)
                .InclusiveBetween(15, 240)
                .WithMessage(
                    "La duración de la sesión debe estar entre 15 y 240 minutos.");

            RuleFor(x => x.EntornoEntrenamiento)
                .IsInEnum()
                .WithMessage(
                    "El entorno de entrenamiento no es válido.");

            RuleFor(x => x.DiasPreferidos)
                .NotNull()
                .WithMessage(
                    "La lista de días preferidos es obligatoria.");

            RuleFor(x => x.EquipamientoIds)
                .NotNull()
                .WithMessage(
                    "La lista de equipamientos es obligatoria.");

            RuleForEach(x => x.DiasPreferidos)
            .IsInEnum();

            RuleForEach(x => x.EquipamientoIds)
            .NotEmpty();

            RuleFor(x => x.DiasPreferidos)
                .Must(dias =>
                    dias is null ||
                    dias.Distinct().Count() == dias.Count)
                .WithMessage(
                    "No se pueden repetir días preferidos.");

            RuleFor(x => x.EquipamientoIds)
                .Must(ids =>
                    ids is null ||
                    ids.Distinct().Count() == ids.Count)
                .WithMessage(
                    "No se pueden repetir equipamientos.");

            RuleFor(x => x)
                .Must(command =>
                    command.DiasPreferidos is null ||
                    command.DiasPreferidos.Count <=
                    command.DiasEntrenamientoPorSemana)
                .WithMessage(
                    "La cantidad de días preferidos no puede superar " +
                    "los días de entrenamiento por semana.");
        }
    }
}
