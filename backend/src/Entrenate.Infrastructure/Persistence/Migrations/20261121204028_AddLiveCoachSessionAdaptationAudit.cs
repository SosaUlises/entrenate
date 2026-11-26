using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entrenate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveCoachSessionAdaptationAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdaptacionesLiveCoachSesion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SesionEntrenamientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    FechaAplicacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EjercicioSesionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SerieEfectivaDesde = table.Column<int>(type: "integer", nullable: true),
                    RepeticionesMinimasAnteriores = table.Column<int>(type: "integer", nullable: true),
                    RepeticionesMaximasAnteriores = table.Column<int>(type: "integer", nullable: true),
                    RepeticionesMinimasNuevas = table.Column<int>(type: "integer", nullable: true),
                    RepeticionesMaximasNuevas = table.Column<int>(type: "integer", nullable: true),
                    RirMinimoAnterior = table.Column<int>(type: "integer", nullable: true),
                    RirMaximoAnterior = table.Column<int>(type: "integer", nullable: true),
                    RirMinimoNuevo = table.Column<int>(type: "integer", nullable: true),
                    RirMaximoNuevo = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptacionesLiveCoachSesion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdaptacionesLiveCoachSesion_SesionesEntrenamiento_SesionEnt~",
                        column: x => x.SesionEntrenamientoId,
                        principalTable: "SesionesEntrenamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdaptacionesLiveCoachSesion_SesionEntrenamientoId_FechaApli~",
                table: "AdaptacionesLiveCoachSesion",
                columns: new[] { "SesionEntrenamientoId", "FechaAplicacionUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdaptacionesLiveCoachSesion");
        }
    }
}
