using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class PerfilEquipamientoConfiguration
    : IEntityTypeConfiguration<PerfilEquipamiento>
    {
        public void Configure(
            EntityTypeBuilder<PerfilEquipamiento> builder)
        {
            builder.ToTable("PerfilEquipamientos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PerfilEntrenamientoId)
                .IsRequired();

            builder.Property(x => x.EquipamientoId)
                .IsRequired();

            builder.HasOne<Equipamiento>()
                .WithMany()
                .HasForeignKey(x => x.EquipamientoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.PerfilEntrenamientoId,
                x.EquipamientoId
            })
            .IsUnique();
        }
    }
}
