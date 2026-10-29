using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class SesionAdaptacionEjercicioExcluidoConfiguration
        : IEntityTypeConfiguration<SesionAdaptacionEjercicioExcluido>
    {
        public void Configure(
            EntityTypeBuilder<SesionAdaptacionEjercicioExcluido> builder)
        {
            builder.ToTable("SesionAdaptacionEjerciciosExcluidos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.SesionAdaptacionId)
                .IsRequired();

            builder.Property(x => x.EjercicioId)
                .IsRequired();

            builder.HasOne(x => x.Ejercicio)
                .WithMany()
                .HasForeignKey(x => x.EjercicioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.SesionAdaptacionId,
                x.EjercicioId
            })
            .IsUnique();
        }
    }
}

