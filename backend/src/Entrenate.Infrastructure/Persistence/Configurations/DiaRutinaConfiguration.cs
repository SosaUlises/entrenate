using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class DiaRutinaConfiguration
    : IEntityTypeConfiguration<DiaRutina>
    {
        public void Configure(EntityTypeBuilder<DiaRutina> builder)
        {
            builder.ToTable("DiasRutina");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.Orden)
                .IsRequired();

            builder.HasMany(x => x.Ejercicios)
                .WithOne(x => x.DiaRutina)
                .HasForeignKey(x => x.DiaRutinaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new
            {
                x.RutinaId,
                x.Orden
            })
            .IsUnique();
        }
    }
}
