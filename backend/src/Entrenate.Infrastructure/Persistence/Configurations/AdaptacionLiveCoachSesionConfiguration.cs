using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public sealed class AdaptacionLiveCoachSesionConfiguration
        : IEntityTypeConfiguration<AdaptacionLiveCoachSesion>
    {
        public void Configure(EntityTypeBuilder<AdaptacionLiveCoachSesion> builder)
        {
            builder.ToTable("AdaptacionesLiveCoachSesion");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Tipo).IsRequired();
            builder.Property(x => x.FechaAplicacionUtc).IsRequired();
            builder.HasIndex(x => new { x.SesionEntrenamientoId, x.FechaAplicacionUtc });
            builder.HasOne(x => x.SesionEntrenamiento)
                .WithMany(x => x.AdaptacionesLiveCoach)
                .HasForeignKey(x => x.SesionEntrenamientoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
