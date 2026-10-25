using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class EjercicioGrupoMuscularSecundarioConfiguration
        : IEntityTypeConfiguration<EjercicioGrupoMuscularSecundario>
    {
        public void Configure(
            EntityTypeBuilder<EjercicioGrupoMuscularSecundario> builder)
        {
            builder.ToTable("EjercicioGruposMuscularesSecundarios");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.GrupoMuscular)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => new
            {
                x.EjercicioId,
                x.GrupoMuscular
            })
            .IsUnique();
        }
    }
}
