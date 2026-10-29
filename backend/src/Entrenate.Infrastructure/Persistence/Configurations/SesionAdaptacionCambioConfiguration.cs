using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class SesionAdaptacionCambioConfiguration
        : IEntityTypeConfiguration<SesionAdaptacionCambio>
    {
        public void Configure(EntityTypeBuilder<SesionAdaptacionCambio> builder)
        {
            builder.ToTable("SesionAdaptacionCambios");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.SesionAdaptacionId)
                .IsRequired();

            builder.Property(x => x.Orden)
                .IsRequired();

            builder.Property(x => x.TipoCambio)
                .IsRequired();

            builder.Property(x => x.Motivo);

            builder.Property(x => x.SourceRoutineExerciseId);

            builder.Property(x => x.EjercicioOriginalId);

            builder.Property(x => x.EjercicioOriginalCodigo)
                .HasMaxLength(100);

            builder.Property(x => x.EjercicioAdaptadoId);

            builder.Property(x => x.EjercicioAdaptadoCodigo)
                .HasMaxLength(100);

            builder.Property(x => x.SeriesOriginal);

            builder.Property(x => x.SeriesAdaptada);

            builder.Property(x => x.RirMinimoOriginal);

            builder.Property(x => x.RirMaximoOriginal);

            builder.Property(x => x.RirMinimoAdaptado);

            builder.Property(x => x.RirMaximoAdaptado);

            builder.HasOne(x => x.EjercicioOriginal)
                .WithMany()
                .HasForeignKey(x => x.EjercicioOriginalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.EjercicioAdaptado)
                .WithMany()
                .HasForeignKey(x => x.EjercicioAdaptadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.SesionAdaptacionId,
                x.Orden
            })
            .IsUnique();
        }
    }
}

