using Entrenate.Domain.Entidades;
using Entrenate.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Entrenate.Infrastructure.Persistence
{
    public class EntrenateDbContext
     : IdentityDbContext<ApplicationUser>
    {
        public EntrenateDbContext(
            DbContextOptions<EntrenateDbContext> options)
            : base(options)
        {
        }

        public DbSet<PerfilEntrenamiento> PerfilesEntrenamiento =>
          Set<PerfilEntrenamiento>();

        public DbSet<Equipamiento> Equipamientos =>
            Set<Equipamiento>();

        public DbSet<PerfilEquipamiento> PerfilEquipamientos =>
            Set<PerfilEquipamiento>();

        public DbSet<DiaEntrenamientoPreferido> DiasEntrenamientoPreferidos =>
            Set<DiaEntrenamientoPreferido>();

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
