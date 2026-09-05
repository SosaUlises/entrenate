using Entrenate.Domain.Entidades;
using Entrenate.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class PerfilEntrenamientoConfiguration
    : IEntityTypeConfiguration<PerfilEntrenamiento>
    {
        public void Configure(
            EntityTypeBuilder<PerfilEntrenamiento> builder)
        {
            builder.ToTable("PerfilesEntrenamiento");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UsuarioId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasIndex(x => x.UsuarioId)
                .IsUnique();

            builder.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<PerfilEntrenamiento>(
                    x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Objetivo)
                .IsRequired();

            builder.Property(x => x.NivelExperiencia)
                .IsRequired();

            builder.Property(x => x.Edad)
                .IsRequired();

            builder.Property(x => x.Sexo)
                .IsRequired(false);

            builder.Property(x => x.PesoKg)
                .HasPrecision(6, 2)
                .IsRequired(false);

            builder.Property(x => x.DiasEntrenamientoPorSemana)
                .IsRequired();

            builder.Property(x => x.DuracionSesionMinutos)
                .IsRequired();

            builder.Property(x => x.EntornoEntrenamiento)
                .IsRequired();

            builder.Property(x => x.CreadoEnUtc)
                .IsRequired();

            builder.Property(x => x.ActualizadoEnUtc)
                .IsRequired();

            builder.HasMany(x => x.DiasPreferidos)
                .WithOne()
                .HasForeignKey(x => x.PerfilEntrenamientoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.DiasPreferidos)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.Equipamientos)
                .WithOne()
                .HasForeignKey(x => x.PerfilEntrenamientoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Equipamientos)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
