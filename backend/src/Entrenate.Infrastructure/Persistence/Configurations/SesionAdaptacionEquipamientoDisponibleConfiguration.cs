using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class SesionAdaptacionEquipamientoDisponibleConfiguration
        : IEntityTypeConfiguration<SesionAdaptacionEquipamientoDisponible>
    {
        public void Configure(
            EntityTypeBuilder<SesionAdaptacionEquipamientoDisponible> builder)
        {
            builder.ToTable("SesionAdaptacionEquipamientosDisponibles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.SesionAdaptacionId)
                .IsRequired();

            builder.Property(x => x.EquipamientoId)
                .IsRequired();

            builder.HasOne(x => x.Equipamiento)
                .WithMany()
                .HasForeignKey(x => x.EquipamientoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.SesionAdaptacionId,
                x.EquipamientoId
            })
            .IsUnique();
        }
    }
}

