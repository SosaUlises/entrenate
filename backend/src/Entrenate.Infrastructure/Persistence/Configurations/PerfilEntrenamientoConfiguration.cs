using Entrenate.Domain.Entidades;
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

            builder.Property(x => x.Sexo)
                .IsRequired(false);

            builder.Property(x => x.FechaNacimiento)
                .IsRequired(false);

            builder.Property(x => x.AlturaCm)
                .HasPrecision(5, 2)
                .IsRequired(false);

            builder.Property(x => x.PesoCorporalKg)
                .HasPrecision(6, 2)
                .IsRequired(false);

            builder.Property(x => x.NivelExperiencia)
                .IsRequired();

            builder.Property(x => x.ObjetivoPrincipal)
                .IsRequired();

            builder.Property(x => x.DiasDisponiblesSemana)
                .IsRequired();

            builder.Property(x => x.DuracionSesionDeseadaMinutos)
                .IsRequired();

            builder.Property(x => x.FechaCreacion)
                .IsRequired();

            builder.Property(x => x.FechaUltimaModificacion)
                .IsRequired();

            builder.HasIndex(x => x.UsuarioId)
                .IsUnique();
        }
    }
}
