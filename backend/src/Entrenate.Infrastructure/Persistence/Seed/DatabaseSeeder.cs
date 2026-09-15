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
            await SeedEjerciciosAsync(cancellationToken);
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

        private async Task SeedEjerciciosAsync(
            CancellationToken cancellationToken)
        {
            var definiciones = new[]
            {
                ("Press banca con barra", "Pecho", new[]
                {
                    "Barra olímpica", "Discos", "Banco plano", "Rack"
                }),
                ("Press banca con mancuernas", "Pecho", new[]
                {
                    "Mancuernas", "Banco plano"
                }),
                ("Press inclinado con mancuernas", "Pecho", new[]
                {
                    "Mancuernas", "Banco ajustable"
                }),
                ("Aperturas con mancuernas", "Pecho", new[]
                {
                    "Mancuernas", "Banco plano"
                }),
                ("Peck deck", "Pecho", new[] { "Peck deck" }),
                ("Flexiones", "Pecho", Array.Empty<string>()),
                ("Dominadas", "Espalda", new[] { "Barra de dominadas" }),
                ("Remo con barra", "Espalda", new[]
                {
                    "Barra olímpica", "Discos"
                }),
                ("Remo con mancuerna", "Espalda", new[] { "Mancuernas" }),
                ("Jalón al pecho", "Espalda", new[] { "Polea alta" }),
                ("Remo en polea baja", "Espalda", new[] { "Polea baja" }),
                ("Press militar con barra", "Hombros", new[]
                {
                    "Barra olímpica", "Discos"
                }),
                ("Press de hombros con mancuernas", "Hombros", new[]
                {
                    "Mancuernas"
                }),
                ("Elevaciones laterales con mancuernas", "Hombros", new[]
                {
                    "Mancuernas"
                }),
                ("Face pull", "Hombros", new[] { "Polea alta" }),
                ("Pike push-up", "Hombros", Array.Empty<string>()),
                ("Curl con mancuernas", "Bíceps", new[] { "Mancuernas" }),
                ("Curl con barra EZ", "Bíceps", new[]
                {
                    "Barra EZ", "Discos"
                }),
                ("Curl en polea", "Bíceps", new[] { "Polea baja" }),
                ("Extensión de tríceps en polea", "Tríceps", new[]
                {
                    "Polea alta"
                }),
                ("Press francés con barra EZ", "Tríceps", new[]
                {
                    "Barra EZ", "Discos"
                }),
                ("Fondos en paralelas", "Tríceps", new[] { "Paralelas" }),
                ("Sentadilla con barra", "Cuádriceps", new[]
                {
                    "Barra olímpica", "Discos", "Rack"
                }),
                ("Sentadilla goblet", "Cuádriceps", new[] { "Mancuernas" }),
                ("Sentadilla con peso corporal", "Cuádriceps",
                    Array.Empty<string>()),
                ("Prensa de piernas", "Cuádriceps", new[]
                {
                    "Prensa de piernas"
                }),
                ("Hack squat", "Cuádriceps", new[] { "Hack squat" }),
                ("Extensión de cuádriceps", "Cuádriceps", new[]
                {
                    "Extensión de cuádriceps"
                }),
                ("Zancadas con mancuernas", "Cuádriceps", new[]
                {
                    "Mancuernas"
                }),
                ("Peso muerto rumano con barra", "Isquiotibiales", new[]
                {
                    "Barra olímpica", "Discos"
                }),
                ("Curl femoral", "Isquiotibiales", new[] { "Curl femoral" }),
                ("Hip thrust con barra", "Glúteos", new[]
                {
                    "Barra olímpica", "Discos", "Banco plano"
                }),
                ("Puente de glúteos", "Glúteos", Array.Empty<string>()),
                ("Elevación de gemelos en máquina", "Gemelos", new[]
                {
                    "Máquina de gemelos"
                }),
                ("Elevación de gemelos de pie", "Gemelos",
                    Array.Empty<string>()),
                ("Plancha", "Core", Array.Empty<string>()),
                ("Crunch abdominal", "Core", Array.Empty<string>())
            };

            var equipamientos = await _context
                .Equipamientos
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var equipamientosPorNombre = equipamientos.ToDictionary(
                x => x.Nombre,
                StringComparer.OrdinalIgnoreCase);

            var equipamientosFaltantes = definiciones
                .SelectMany(x => x.Item3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !equipamientosPorNombre.ContainsKey(x))
                .OrderBy(x => x)
                .ToList();

            if (equipamientosFaltantes.Count > 0)
            {
                throw new InvalidOperationException(
                    "No se pueden sembrar los ejercicios porque faltan " +
                    "equipamientos en el catálogo: " +
                    string.Join(", ", equipamientosFaltantes));
            }

            var nombresExistentes = await _context
                .Ejercicios
                .Select(x => x.Nombre)
                .ToListAsync(cancellationToken);

            var nombresSet = new HashSet<string>(
                nombresExistentes,
                StringComparer.OrdinalIgnoreCase);

            var nuevosEjercicios = definiciones
                .Where(x => !nombresSet.Contains(x.Item1))
                .Select(definicion =>
                {
                    var ejercicio = new Ejercicio
                    {
                        Id = Guid.NewGuid(),
                        Nombre = definicion.Item1,
                        Descripcion = null,
                        GrupoMuscularPrincipal = definicion.Item2,
                        WorkoutGuideId = null,
                        ImagenPrincipalUrl = null,
                        Activo = true
                    };

                    var equipamientoIds = definicion.Item3
                        .Select(x => equipamientosPorNombre[x].Id);

                    ejercicio.DefinirEquipamientos(equipamientoIds);

                    return ejercicio;
                })
                .ToList();

            if (nuevosEjercicios.Count == 0)
            {
                _logger.LogInformation(
                    "El catálogo de ejercicios ya está actualizado.");

                return;
            }

            await _context.Ejercicios.AddRangeAsync(
                nuevosEjercicios,
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Se agregaron {Cantidad} ejercicios al catálogo.",
                nuevosEjercicios.Count);
        }
    }
}
