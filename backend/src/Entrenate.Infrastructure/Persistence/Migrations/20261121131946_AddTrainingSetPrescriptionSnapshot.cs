using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entrenate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingSetPrescriptionSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RepeticionesObjetivoMaximas",
                table: "SeriesEntrenamiento",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeticionesObjetivoMinimas",
                table: "SeriesEntrenamiento",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RirObjetivoMaximo",
                table: "SeriesEntrenamiento",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RirObjetivoMinimo",
                table: "SeriesEntrenamiento",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepeticionesObjetivoMaximas",
                table: "SeriesEntrenamiento");

            migrationBuilder.DropColumn(
                name: "RepeticionesObjetivoMinimas",
                table: "SeriesEntrenamiento");

            migrationBuilder.DropColumn(
                name: "RirObjetivoMaximo",
                table: "SeriesEntrenamiento");

            migrationBuilder.DropColumn(
                name: "RirObjetivoMinimo",
                table: "SeriesEntrenamiento");
        }
    }
}
