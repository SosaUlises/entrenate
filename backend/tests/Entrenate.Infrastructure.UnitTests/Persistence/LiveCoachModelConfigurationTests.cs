using System;
using System.Linq;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using Entrenate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Entrenate.Infrastructure.UnitTests.Persistence
{
    public sealed class LiveCoachModelConfigurationTests
    {
        [Fact]
        public void Model_ConfiguresSerieEntrenamientoPrescriptionSnapshotCorrectly()
        {
            var options = new DbContextOptionsBuilder<EntrenateDbContext>()
                .UseNpgsql()
                .Options;

            using var context = new EntrenateDbContext(options);
            var model = context.Model;

            var entityType = model.FindEntityType(typeof(SerieEntrenamiento));
            Assert.NotNull(entityType);

            var propRepMin = entityType.FindProperty(nameof(SerieEntrenamiento.RepeticionesObjetivoMinimas));
            Assert.NotNull(propRepMin);
            Assert.True(propRepMin.IsNullable);

            var propRepMax = entityType.FindProperty(nameof(SerieEntrenamiento.RepeticionesObjetivoMaximas));
            Assert.NotNull(propRepMax);
            Assert.True(propRepMax.IsNullable);

            var propRirMin = entityType.FindProperty(nameof(SerieEntrenamiento.RirObjetivoMinimo));
            Assert.NotNull(propRirMin);
            Assert.True(propRirMin.IsNullable);

            var propRirMax = entityType.FindProperty(nameof(SerieEntrenamiento.RirObjetivoMaximo));
            Assert.NotNull(propRirMax);
            Assert.True(propRirMax.IsNullable);
        }

        [Fact]
        public void Model_ConfiguresAdaptacionLiveCoachSesionCorrectly()
        {
            var options = new DbContextOptionsBuilder<EntrenateDbContext>()
                .UseNpgsql()
                .Options;

            using var context = new EntrenateDbContext(options);
            var model = context.Model;

            var entityType = model.FindEntityType(typeof(AdaptacionLiveCoachSesion));
            Assert.NotNull(entityType);
            Assert.Equal("AdaptacionesLiveCoachSesion", entityType.GetTableName());

            var fk = entityType.GetForeignKeys().FirstOrDefault(item =>
                item.PrincipalEntityType.ClrType == typeof(SesionEntrenamiento));
            Assert.NotNull(fk);
            Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior);

            var index = entityType.GetIndexes().FirstOrDefault(item =>
                item.Properties.Count == 2 &&
                item.Properties.Any(p => p.Name == nameof(AdaptacionLiveCoachSesion.SesionEntrenamientoId)) &&
                item.Properties.Any(p => p.Name == nameof(AdaptacionLiveCoachSesion.FechaAplicacionUtc)));
            Assert.NotNull(index);
        }

        [Fact]
        public void Model_WhenAdaptacionLiveCoachRegisteredOnTrackedSession_DetectChangesMarksAsAdded()
        {
            var options = new DbContextOptionsBuilder<EntrenateDbContext>()
                .UseNpgsql()
                .Options;

            using var context = new EntrenateDbContext(options);

            var exercise = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = "Press banca",
                GrupoMuscularPrincipal = "Pecho",
                Activo = true
            };
            var exerciseSession = new EjercicioSesion(
                exercise.Id,
                orden: 1,
                seriesObjetivo: 3,
                repeticionesMinimasObjetivo: 8,
                repeticionesMaximasObjetivo: 12,
                rirObjetivoMinimo: 1,
                rirObjetivoMaximo: 2,
                descansoObjetivoSegundos: 90);

            var session = new SesionEntrenamiento(
                "test-user",
                Guid.NewGuid(),
                DateTime.UtcNow,
                new[] { exerciseSession });

            // Attach session as if loaded from DB in Unchanged state
            context.Attach(session);
            Assert.Equal(EntityState.Unchanged, context.Entry(session).State);

            session.RegistrarAdaptacionLiveCoach(TipoAdaptacionLiveCoach.Tiempo, DateTime.UtcNow);

            context.ChangeTracker.DetectChanges();

            var adaptation = Assert.Single(session.AdaptacionesLiveCoach);
            var entry = context.Entry(adaptation);
            Assert.Equal(EntityState.Added, entry.State);
        }
    }
}
