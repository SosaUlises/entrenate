using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class EjercicioRutinaConfiguration
     : IEntityTypeConfiguration<EjercicioRutina>
    {
        public void Configure(EntityTypeBuilder<EjercicioRutina> builder)
        {
            builder.ToTable("EjerciciosRutina");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CantidadSeries)
                .IsRequired();

            builder.Property(x => x.RepeticionesMinimas)
                .IsRequired();

            builder.Property(x => x.RepeticionesMaximas)
                .IsRequired();

            builder.Property(x => x.RirObjetivoMinimo)
                .IsRequired();

            builder.Property(x => x.RirObjetivoMaximo)
                .IsRequired();

            builder.Property(x => x.DescansoSegundos)
                .IsRequired();

            builder.Property(x => x.Orden)
                .IsRequired();

            builder.Property(x => x.Notas)
                .HasMaxLength(500);

            builder.HasOne(x => x.Ejercicio)
                .WithMany()
                .HasForeignKey(x => x.EjercicioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.DiaRutinaId,
                x.Orden
            })
            .IsUnique();
        }
    }
}
