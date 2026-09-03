using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class EjercicioConfiguration
     : IEntityTypeConfiguration<Ejercicio>
    {
        public void Configure(EntityTypeBuilder<Ejercicio> builder)
        {
            builder.ToTable("Ejercicios");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Descripcion)
                .HasMaxLength(1000);

            builder.Property(x => x.GrupoMuscularPrincipal)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Equipamiento)
                .HasMaxLength(100);

            builder.Property(x => x.WorkoutGuideId)
                .HasMaxLength(200);

            builder.Property(x => x.ImagenPrincipalUrl)
                .HasMaxLength(1000);

            builder.Property(x => x.Activo)
                .IsRequired();

            builder.HasIndex(x => x.WorkoutGuideId)
                .IsUnique();
        }
    }
}
