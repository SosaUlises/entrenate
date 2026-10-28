using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrenate.Infrastructure.Persistence.Configurations
{
    public class SesionAdaptacionConfiguration
        : IEntityTypeConfiguration<SesionAdaptacion>
    {
        public void Configure(EntityTypeBuilder<SesionAdaptacion> builder)
        {
            builder.ToTable("SesionesAdaptacion");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.SesionEntrenamientoId)
                .IsRequired();

            builder.Property(x => x.UsuarioId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.PolicyVersion)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.AdaptationProposalId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.ProposalHash)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.AvailableMinutes);

            builder.Property(x => x.EffortPreference)
                .IsRequired();

            builder.Property(x => x.Factibilidad)
                .IsRequired();

            builder.Property(x => x.FechaConfirmacionUtc)
                .IsRequired();

            builder.HasOne(x => x.SesionEntrenamiento)
                .WithOne(x => x.Adaptacion)
                .HasForeignKey<SesionAdaptacion>(x => x.SesionEntrenamientoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Cambios)
                .WithOne(x => x.SesionAdaptacion)
                .HasForeignKey(x => x.SesionAdaptacionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Cambios)
                .HasField("_cambios")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.EquipamientosDisponibles)
                .WithOne(x => x.SesionAdaptacion)
                .HasForeignKey(x => x.SesionAdaptacionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.EquipamientosDisponibles)
                .HasField("_equipamientosDisponibles")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.EjerciciosExcluidos)
                .WithOne(x => x.SesionAdaptacion)
                .HasForeignKey(x => x.SesionAdaptacionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.EjerciciosExcluidos)
                .HasField("_ejerciciosExcluidos")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(x => x.SesionEntrenamientoId)
                .IsUnique();

            builder.HasIndex(x => new
            {
                x.UsuarioId,
                x.AdaptationProposalId
            })
            .HasDatabaseName("IX_SesionesAdaptacion_UsuarioId_AdaptationProposalId")
            .IsUnique();

            builder.HasIndex(x => x.ProposalHash)
                .HasDatabaseName("IX_SesionesAdaptacion_ProposalHash");
        }
    }
}
