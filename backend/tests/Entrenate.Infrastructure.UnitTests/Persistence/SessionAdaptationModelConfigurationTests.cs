using Entrenate.Domain.Entidades;
using Entrenate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Entrenate.Infrastructure.UnitTests.Persistence
{
    public class SessionAdaptationModelConfigurationTests
    {
        [Fact]
        public void Model_ConfiguresSesionAdaptacionCorrectly()
        {
            var options = new DbContextOptionsBuilder<EntrenateDbContext>()
                .UseNpgsql("Host=localhost;Database=entrenate_test;Username=postgres;Password=postgres")
                .Options;

            using var context = new EntrenateDbContext(options);
            var model = context.Model;

            var entityType = model.FindEntityType(typeof(SesionAdaptacion));
            Assert.NotNull(entityType);
            Assert.Equal("SesionesAdaptacion", entityType.GetTableName());

            var propUsuario = entityType.FindProperty(nameof(SesionAdaptacion.UsuarioId));
            Assert.NotNull(propUsuario);
            Assert.False(propUsuario.IsNullable);
            Assert.Equal(450, propUsuario.GetMaxLength());

            var propProposalId = entityType.FindProperty(nameof(SesionAdaptacion.AdaptationProposalId));
            Assert.NotNull(propProposalId);
            Assert.False(propProposalId.IsNullable);
            Assert.Equal(100, propProposalId.GetMaxLength());

            var propHash = entityType.FindProperty(nameof(SesionAdaptacion.ProposalHash));
            Assert.NotNull(propHash);
            Assert.False(propHash.IsNullable);
            Assert.Equal(128, propHash.GetMaxLength());

            var uniqueProposalIndex = entityType.GetIndexes().FirstOrDefault(i =>
                i.GetDatabaseName() == "IX_SesionesAdaptacion_UsuarioId_AdaptationProposalId");
            Assert.NotNull(uniqueProposalIndex);
            Assert.True(uniqueProposalIndex.IsUnique);
            Assert.Equal(
                new[] { nameof(SesionAdaptacion.UsuarioId), nameof(SesionAdaptacion.AdaptationProposalId) },
                uniqueProposalIndex.Properties.Select(p => p.Name));

            var hashIndex = entityType.GetIndexes().FirstOrDefault(i =>
                i.GetDatabaseName() == "IX_SesionesAdaptacion_ProposalHash");
            Assert.NotNull(hashIndex);
            Assert.False(hashIndex.IsUnique);

            var sesionFk = entityType.GetForeignKeys().FirstOrDefault(fk =>
                fk.PrincipalEntityType.ClrType == typeof(SesionEntrenamiento));
            Assert.NotNull(sesionFk);
            Assert.True(sesionFk.IsUnique);
            Assert.Equal(DeleteBehavior.Cascade, sesionFk.DeleteBehavior);
        }

        [Fact]
        public void Model_ConfiguresChildCollectionsCorrectly()
        {
            var options = new DbContextOptionsBuilder<EntrenateDbContext>()
                .UseNpgsql("Host=localhost;Database=entrenate_test;Username=postgres;Password=postgres")
                .Options;

            using var context = new EntrenateDbContext(options);
            var model = context.Model;

            var cambioType = model.FindEntityType(typeof(SesionAdaptacionCambio));
            Assert.NotNull(cambioType);
            Assert.Equal("SesionAdaptacionCambios", cambioType.GetTableName());

            var cambioOrderIndex = cambioType.GetIndexes().FirstOrDefault(i => i.Properties.Count == 2
                && i.Properties.Any(p => p.Name == nameof(SesionAdaptacionCambio.SesionAdaptacionId))
                && i.Properties.Any(p => p.Name == nameof(SesionAdaptacionCambio.Orden)));
            Assert.NotNull(cambioOrderIndex);
            Assert.True(cambioOrderIndex.IsUnique);

            var eqDispType = model.FindEntityType(typeof(SesionAdaptacionEquipamientoDisponible));
            Assert.NotNull(eqDispType);
            Assert.Equal("SesionAdaptacionEquipamientosDisponibles", eqDispType.GetTableName());

            var ejExclType = model.FindEntityType(typeof(SesionAdaptacionEjercicioExcluido));
            Assert.NotNull(ejExclType);
            Assert.Equal("SesionAdaptacionEjerciciosExcluidos", ejExclType.GetTableName());
        }
    }
}
