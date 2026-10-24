using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entrenate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingSessionActivityIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SesionesEntrenamiento_UsuarioId_Estado_HoraInicio",
                table: "SesionesEntrenamiento",
                columns: new[] { "UsuarioId", "Estado", "HoraInicio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SesionesEntrenamiento_UsuarioId_Estado_HoraInicio",
                table: "SesionesEntrenamiento");
        }
    }
}
