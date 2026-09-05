using Entrenate.Domain.Enums;

namespace Entrenate.Domain.Entidades
{
    public class Equipamiento
    {
        public Guid Id { get; private set; }

        public string Nombre { get; private set; } = string.Empty;

        public CategoriaEquipamiento Categoria { get; private set; }

        public bool Activo { get; private set; }

        private Equipamiento()
        {
        }

        public Equipamiento(
            string nombre,
            CategoriaEquipamiento categoria)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new ArgumentException(
                    "El nombre del equipamiento es obligatorio.",
                    nameof(nombre));
            }

            Id = Guid.NewGuid();
            Nombre = nombre.Trim();
            Categoria = categoria;
            Activo = true;
        }

        public void Desactivar()
        {
            Activo = false;
        }

        public void Activar()
        {
            Activo = true;
        }
    }
}
