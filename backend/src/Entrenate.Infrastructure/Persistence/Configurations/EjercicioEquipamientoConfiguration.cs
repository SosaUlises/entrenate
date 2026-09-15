using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class EjercicioEquipamientoConfiguration
    : IEntityTypeConfiguration<EjercicioEquipamiento>
    {
        public void Configure(
            EntityTypeBuilder<EjercicioEquipamiento> builder)
        {
            builder.ToTable("EjercicioEquipamientos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.EjercicioId)
                .IsRequired();

            builder.Property(x => x.EquipamientoId)
                .IsRequired();

            builder.HasOne<Equipamiento>()
                .WithMany()
                .HasForeignKey(x => x.EquipamientoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.EjercicioId,
                x.EquipamientoId
            })
            .IsUnique();
        }
    }
}
