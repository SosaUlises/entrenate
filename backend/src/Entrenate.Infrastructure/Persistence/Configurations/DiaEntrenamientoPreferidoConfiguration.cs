using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class DiaEntrenamientoPreferidoConfiguration
    : IEntityTypeConfiguration<DiaEntrenamientoPreferido>
    {
        public void Configure(
            EntityTypeBuilder<DiaEntrenamientoPreferido> builder)
        {
            builder.ToTable("DiasEntrenamientoPreferidos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.PerfilEntrenamientoId)
                .IsRequired();

            builder.Property(x => x.Dia)
                .IsRequired();

            builder.HasIndex(x => new
            {
                x.PerfilEntrenamientoId,
                x.Dia
            })
            .IsUnique();
        }
    }
}
