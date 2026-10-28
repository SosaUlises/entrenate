using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entrenate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutineExerciseAdaptationIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRequiredForAdaptation",
                table: "EjerciciosRutina",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRequiredForAdaptation",
                table: "EjerciciosRutina");
        }
    }
}
