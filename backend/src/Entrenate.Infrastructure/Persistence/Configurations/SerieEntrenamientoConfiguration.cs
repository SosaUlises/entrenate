using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class SerieEntrenamientoConfiguration
    : IEntityTypeConfiguration<SerieEntrenamiento>
    {
        public void Configure(EntityTypeBuilder<SerieEntrenamiento> builder)
        {
            builder.ToTable("SeriesEntrenamiento");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.NumeroSerie)
                .IsRequired();

            builder.Property(x => x.Peso)
                .HasPrecision(7, 2)
                .IsRequired();

            builder.Property(x => x.Repeticiones)
                .IsRequired();

            builder.Property(x => x.Rir);

            builder.Property(x => x.Completada)
                .IsRequired();

            builder.Property(x => x.FechaHoraRegistro)
                .IsRequired();

            builder.HasIndex(x => new
            {
                x.EjercicioSesionId,
                x.NumeroSerie
            })
            .IsUnique();
        }
    }
}
