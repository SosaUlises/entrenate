using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class EquipamientoConfiguration
    : IEntityTypeConfiguration<Equipamiento>
    {
        public void Configure(
            EntityTypeBuilder<Equipamiento> builder)
        {
            builder.ToTable("Equipamientos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Categoria)
                .IsRequired();

            builder.Property(x => x.Activo)
                .IsRequired();

            builder.HasIndex(x => x.Nombre)
                .IsUnique();
        }
    }
}
