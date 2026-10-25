using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entrenate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutinePlannerV2Metadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlannerProposalHash",
                table: "Rutinas",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlannerProposalId",
                table: "Rutinas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Ejercicios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatronMovimiento",
                table: "Ejercicios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoEjercicio",
                table: "Ejercicios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiaSemana",
                table: "DiasRutina",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EjercicioGruposMuscularesSecundarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EjercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrupoMuscular = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjercicioGruposMuscularesSecundarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EjercicioGruposMuscularesSecundarios_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rutinas_UsuarioId_PlannerProposalId",
                table: "Rutinas",
                columns: new[] { "UsuarioId", "PlannerProposalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ejercicios_Codigo",
                table: "Ejercicios",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiasRutina_RutinaId_DiaSemana",
                table: "DiasRutina",
                columns: new[] { "RutinaId", "DiaSemana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EjercicioGruposMuscularesSecundarios_EjercicioId_GrupoMuscu~",
                table: "EjercicioGruposMuscularesSecundarios",
                columns: new[] { "EjercicioId", "GrupoMuscular" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EjercicioGruposMuscularesSecundarios");

            migrationBuilder.DropIndex(
                name: "IX_Rutinas_UsuarioId_PlannerProposalId",
                table: "Rutinas");

            migrationBuilder.DropIndex(
                name: "IX_Ejercicios_Codigo",
                table: "Ejercicios");

            migrationBuilder.DropIndex(
                name: "IX_DiasRutina_RutinaId_DiaSemana",
                table: "DiasRutina");

            migrationBuilder.DropColumn(
                name: "PlannerProposalHash",
                table: "Rutinas");

            migrationBuilder.DropColumn(
                name: "PlannerProposalId",
                table: "Rutinas");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Ejercicios");

            migrationBuilder.DropColumn(
                name: "PatronMovimiento",
                table: "Ejercicios");

            migrationBuilder.DropColumn(
                name: "TipoEjercicio",
                table: "Ejercicios");

            migrationBuilder.DropColumn(
                name: "DiaSemana",
                table: "DiasRutina");
        }
    }
}
