using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entrenate.Infrastructure.Persistence.Seed
{
    public class DatabaseSeeder
    {
        private readonly EntrenateDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(
            EntrenateDbContext context,
            ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(
            CancellationToken cancellationToken = default)
        {
            await SeedEquipamientosAsync(cancellationToken);
        }

        private async Task SeedEquipamientosAsync(
            CancellationToken cancellationToken)
        {
            var equipamientos = new[]
            {
            new Equipamiento(
                "Mancuernas",
                CategoriaEquipamiento.PesasLibres),

            new Equipamiento(
                "Pesas rusas",
                CategoriaEquipamiento.PesasLibres),

            new Equipamiento(
                "Discos",
                CategoriaEquipamiento.PesasLibres),

            new Equipamiento(
                "Barra olímpica",
                CategoriaEquipamiento.Barras),

            new Equipamiento(
                "Barra EZ",
                CategoriaEquipamiento.Barras),

            new Equipamiento(
                "Barra hexagonal",
                CategoriaEquipamiento.Barras),

            new Equipamiento(
                "Banco plano",
                CategoriaEquipamiento.BancosYRacks),

            new Equipamiento(
                "Banco ajustable",
                CategoriaEquipamiento.BancosYRacks),

            new Equipamiento(
                "Rack",
                CategoriaEquipamiento.BancosYRacks),

            new Equipamiento(
                "Jaula de potencia",
                CategoriaEquipamiento.BancosYRacks),

            new Equipamiento(
                "Polea alta",
                CategoriaEquipamiento.Poleas),

            new Equipamiento(
                "Polea baja",
                CategoriaEquipamiento.Poleas),

            new Equipamiento(
                "Polea doble",
                CategoriaEquipamiento.Poleas),

            new Equipamiento(
                "Máquina Smith",
                CategoriaEquipamiento.Maquinas),

            new Equipamiento(
                "Prensa de piernas",
                CategoriaEquipamiento.Maquinas),

            new Equipamiento(
                "Hack squat",
                CategoriaEquipamiento.Maquinas),

            new Equipamiento(
                "Extensión de cuádriceps",
                CategoriaEquipamiento.Maquinas),

            new Equipamiento(
                "Curl femoral",
                CategoriaEquipamiento.Maquinas),

            new Equipamiento(
                "Peck deck",
                CategoriaEquipamiento.Maquinas),

            new Equipamiento(
                "Máquina de gemelos",
                CategoriaEquipamiento.Maquinas),

            new Equipamiento(
                "Barra de dominadas",
                CategoriaEquipamiento.Calistenia),

            new Equipamiento(
                "Paralelas",
                CategoriaEquipamiento.Calistenia),

            new Equipamiento(
                "Anillas",
                CategoriaEquipamiento.Calistenia),

            new Equipamiento(
                "Bandas elásticas",
                CategoriaEquipamiento.Otros)
        };

            var nombresExistentes = await _context
                .Equipamientos
                .Select(x => x.Nombre)
                .ToListAsync(cancellationToken);

            var nombresSet = new HashSet<string>(
                nombresExistentes,
                StringComparer.OrdinalIgnoreCase);

            var nuevosEquipamientos = equipamientos
                .Where(x => !nombresSet.Contains(x.Nombre))
                .ToList();

            if (nuevosEquipamientos.Count == 0)
            {
                _logger.LogInformation(
                    "El catálogo de equipamientos ya está actualizado.");

                return;
            }

            await _context.Equipamientos.AddRangeAsync(
                nuevosEquipamientos,
                cancellationToken);

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Se agregaron {Cantidad} equipamientos al catálogo.",
                nuevosEquipamientos.Count);
        }
    }
}
