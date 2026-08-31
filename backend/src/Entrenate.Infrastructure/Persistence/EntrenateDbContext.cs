using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Entrenate.Infrastructure.Persistence
{
    public class EntrenateDbContext : DbContext
    {
        public EntrenateDbContext(
            DbContextOptions<EntrenateDbContext> options)
            : base(options)
        {
        }

        public DbSet<PerfilEntrenamiento> PerfilesEntrenamiento =>
            Set<PerfilEntrenamiento>();

        public DbSet<Ejercicio> Ejercicios =>
            Set<Ejercicio>();

        public DbSet<Rutina> Rutinas =>
            Set<Rutina>();

        public DbSet<DiaRutina> DiasRutina =>
            Set<DiaRutina>();

        public DbSet<EjercicioRutina> EjerciciosRutina =>
            Set<EjercicioRutina>();

        public DbSet<SesionEntrenamiento> SesionesEntrenamiento =>
            Set<SesionEntrenamiento>();

        public DbSet<EjercicioSesion> EjerciciosSesion =>
            Set<EjercicioSesion>();

        public DbSet<SerieEntrenamiento> SeriesEntrenamiento =>
            Set<SerieEntrenamiento>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(EntrenateDbContext).Assembly);
        }
    }
}
