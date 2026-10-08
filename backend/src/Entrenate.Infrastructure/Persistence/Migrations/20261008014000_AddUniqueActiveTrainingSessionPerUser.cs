using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entrenate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveTrainingSessionPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SesionesEntrenamiento_UsuarioId_Active",
                table: "SesionesEntrenamiento",
                column: "UsuarioId",
                unique: true,
                filter: "\"Estado\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SesionesEntrenamiento_UsuarioId_Active",
                table: "SesionesEntrenamiento");
        }
    }
}
