using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class EjercicioSesionConfiguration
     : IEntityTypeConfiguration<EjercicioSesion>
    {
        public void Configure(EntityTypeBuilder<EjercicioSesion> builder)
        {
            builder.ToTable("EjerciciosSesion");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Orden)
                .IsRequired();

            builder.Property(x => x.SeriesObjetivo)
                .IsRequired();

            builder.Property(x => x.RepeticionesMinimasObjetivo)
                .IsRequired();

            builder.Property(x => x.RepeticionesMaximasObjetivo)
                .IsRequired();

            builder.Property(x => x.RirObjetivoMinimo)
                .IsRequired();

            builder.Property(x => x.RirObjetivoMaximo)
                .IsRequired();

            builder.Property(x => x.DescansoObjetivoSegundos)
                .IsRequired();

            builder.Property(x => x.Notas)
                .HasMaxLength(500);

            builder.HasOne(x => x.Ejercicio)
                .WithMany()
                .HasForeignKey(x => x.EjercicioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Series)
                .WithOne(x => x.EjercicioSesion)
                .HasForeignKey(x => x.EjercicioSesionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new
            {
                x.SesionEntrenamientoId,
                x.Orden
            })
            .IsUnique();
        }
    }
}
