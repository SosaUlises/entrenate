using Entrenate.Domain.Enums;

namespace Entrenate.Domain.Entidades
{
    public class PerfilEntrenamiento
    {
        private readonly List<DiaEntrenamientoPreferido> _diasPreferidos = [];
        private readonly List<PerfilEquipamiento> _equipamientos = [];

        public Guid Id { get; private set; }

        public string UsuarioId { get; private set; } = string.Empty;

        public ObjetivoEntrenamiento Objetivo { get; private set; }

        public NivelExperiencia NivelExperiencia { get; private set; }

        public int Edad { get; private set; }

        public Sexo? Sexo { get; private set; }

        public decimal? PesoKg { get; private set; }

        public int DiasEntrenamientoPorSemana { get; private set; }

        public int DuracionSesionMinutos { get; private set; }

        public EntornoEntrenamiento EntornoEntrenamiento { get; private set; }

        public DateTime CreadoEnUtc { get; private set; }

        public DateTime ActualizadoEnUtc { get; private set; }

        public IReadOnlyCollection<DiaEntrenamientoPreferido> DiasPreferidos
            => _diasPreferidos.AsReadOnly();

        public IReadOnlyCollection<PerfilEquipamiento> Equipamientos
            => _equipamientos.AsReadOnly();

        private PerfilEntrenamiento()
        {
        }

        public PerfilEntrenamiento(
            string usuarioId,
            ObjetivoEntrenamiento objetivo,
            NivelExperiencia nivelExperiencia,
            int edad,
            Sexo? sexo,
            decimal? pesoKg,
            int diasEntrenamientoPorSemana,
            int duracionSesionMinutos,
            EntornoEntrenamiento entornoEntrenamiento)
        {
            ValidarDatos(
                usuarioId,
                edad,
                pesoKg,
                diasEntrenamientoPorSemana,
                duracionSesionMinutos);

            Id = Guid.NewGuid();

            UsuarioId = usuarioId;
            Objetivo = objetivo;
            NivelExperiencia = nivelExperiencia;
            Edad = edad;
            Sexo = sexo;
            PesoKg = pesoKg;
            DiasEntrenamientoPorSemana = diasEntrenamientoPorSemana;
            DuracionSesionMinutos = duracionSesionMinutos;
            EntornoEntrenamiento = entornoEntrenamiento;

            CreadoEnUtc = DateTime.UtcNow;
            ActualizadoEnUtc = DateTime.UtcNow;
        }

        public void Actualizar(
            ObjetivoEntrenamiento objetivo,
            NivelExperiencia nivelExperiencia,
            int edad,
            Sexo? sexo,
            decimal? pesoKg,
            int diasEntrenamientoPorSemana,
            int duracionSesionMinutos,
            EntornoEntrenamiento entornoEntrenamiento)
        {
            ValidarDatos(
                UsuarioId,
                edad,
                pesoKg,
                diasEntrenamientoPorSemana,
                duracionSesionMinutos);

            Objetivo = objetivo;
            NivelExperiencia = nivelExperiencia;
            Edad = edad;
            Sexo = sexo;
            PesoKg = pesoKg;
            DiasEntrenamientoPorSemana = diasEntrenamientoPorSemana;
            DuracionSesionMinutos = duracionSesionMinutos;
            EntornoEntrenamiento = entornoEntrenamiento;

            ActualizadoEnUtc = DateTime.UtcNow;
        }

        public void DefinirDiasPreferidos(
            IEnumerable<DiaSemana> dias)
        {
            ArgumentNullException.ThrowIfNull(dias);

            var diasUnicos = dias
                .Distinct()
                .ToHashSet();

            if (diasUnicos.Count > DiasEntrenamientoPorSemana)
            {
                throw new InvalidOperationException(
                    "La cantidad de días preferidos no puede superar " +
                    "la cantidad de días de entrenamiento por semana.");
            }

            _diasPreferidos.RemoveAll(
                actual => !diasUnicos.Contains(actual.Dia));

            var diasExistentes = _diasPreferidos
                .Select(x => x.Dia)
                .ToHashSet();

            foreach (var dia in diasUnicos)
            {
                if (diasExistentes.Contains(dia))
                {
                    continue;
                }

                _diasPreferidos.Add(
                    new DiaEntrenamientoPreferido(
                        Id,
                        dia));
            }

            ActualizadoEnUtc = DateTime.UtcNow;
        }

        public void DefinirEquipamientos(
           IEnumerable<Guid> equipamientoIds)
        {
            ArgumentNullException.ThrowIfNull(equipamientoIds);

            var idsUnicos = equipamientoIds
                .Distinct()
                .ToHashSet();

            _equipamientos.RemoveAll(
                actual =>
                    !idsUnicos.Contains(actual.EquipamientoId));

            var idsExistentes = _equipamientos
                .Select(x => x.EquipamientoId)
                .ToHashSet();

            foreach (var equipamientoId in idsUnicos)
            {
                if (idsExistentes.Contains(equipamientoId))
                {
                    continue;
                }

                _equipamientos.Add(
                    new PerfilEquipamiento(
                        Id,
                        equipamientoId));
            }

            ActualizadoEnUtc = DateTime.UtcNow;
        }

        private static void ValidarDatos(
            string usuarioId,
            int edad,
            decimal? pesoKg,
            int diasEntrenamientoPorSemana,
            int duracionSesionMinutos)
        {
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                throw new ArgumentException(
                    "El usuario es obligatorio.",
                    nameof(usuarioId));
            }

            if (edad <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(edad),
                    "La edad debe ser mayor a cero.");
            }

            if (pesoKg is <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pesoKg),
                    "El peso debe ser mayor a cero.");
            }

            if (diasEntrenamientoPorSemana is < 1 or > 7)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(diasEntrenamientoPorSemana),
                    "Los días de entrenamiento deben estar entre 1 y 7.");
            }

            if (duracionSesionMinutos <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duracionSesionMinutos),
                    "La duración de la sesión debe ser mayor a cero.");
            }
        }
    }
}
