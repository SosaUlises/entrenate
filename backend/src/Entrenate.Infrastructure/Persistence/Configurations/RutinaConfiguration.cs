using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class RutinaConfiguration
    : IEntityTypeConfiguration<Rutina>
    {
        public void Configure(EntityTypeBuilder<Rutina> builder)
        {
            builder.ToTable("Rutinas");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UsuarioId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.FechaCreacion)
                .IsRequired();

            builder.Property(x => x.FechaUltimaModificacion)
                .IsRequired();

            builder.Property(x => x.Activa)
                .IsRequired();

            builder.HasMany(x => x.Dias)
                .WithOne(x => x.Rutina)
                .HasForeignKey(x => x.RutinaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UsuarioId);
        }
    }
}
