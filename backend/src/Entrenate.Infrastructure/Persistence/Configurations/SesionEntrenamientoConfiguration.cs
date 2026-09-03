using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class SesionEntrenamientoConfiguration
    : IEntityTypeConfiguration<SesionEntrenamiento>
    {
        public void Configure(
            EntityTypeBuilder<SesionEntrenamiento> builder)
        {
            builder.ToTable("SesionesEntrenamiento");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UsuarioId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Fecha)
                .IsRequired();

            builder.Property(x => x.HoraInicio)
                .IsRequired();

            builder.Property(x => x.Estado)
                .IsRequired();

            builder.Property(x => x.Notas)
                .HasMaxLength(1000);

            builder.HasOne(x => x.DiaRutina)
                .WithMany()
                .HasForeignKey(x => x.DiaRutinaId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Ejercicios)
                .WithOne(x => x.SesionEntrenamiento)
                .HasForeignKey(x => x.SesionEntrenamientoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new
            {
                x.UsuarioId,
                x.Fecha
            });
        }
    }
}
