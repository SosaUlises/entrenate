using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entrenate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingProfileOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlturaCm",
                table: "PerfilesEntrenamiento");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "PerfilesEntrenamiento");

            migrationBuilder.RenameColumn(
                name: "PesoCorporalKg",
                table: "PerfilesEntrenamiento",
                newName: "PesoKg");

            migrationBuilder.RenameColumn(
                name: "ObjetivoPrincipal",
                table: "PerfilesEntrenamiento",
                newName: "Objetivo");

            migrationBuilder.RenameColumn(
                name: "FechaUltimaModificacion",
                table: "PerfilesEntrenamiento",
                newName: "CreadoEnUtc");

            migrationBuilder.RenameColumn(
                name: "FechaCreacion",
                table: "PerfilesEntrenamiento",
                newName: "ActualizadoEnUtc");

            migrationBuilder.RenameColumn(
                name: "DuracionSesionDeseadaMinutos",
                table: "PerfilesEntrenamiento",
                newName: "EntornoEntrenamiento");

            migrationBuilder.RenameColumn(
                name: "DiasDisponiblesSemana",
                table: "PerfilesEntrenamiento",
                newName: "Edad");

            migrationBuilder.AddColumn<int>(
                name: "DiasEntrenamientoPorSemana",
                table: "PerfilesEntrenamiento",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DuracionSesionMinutos",
                table: "PerfilesEntrenamiento",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DiasEntrenamientoPreferidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEntrenamientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dia = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiasEntrenamientoPreferidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiasEntrenamientoPreferidos_PerfilesEntrenamiento_PerfilEnt~",
                        column: x => x.PerfilEntrenamientoId,
                        principalTable: "PerfilesEntrenamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipamientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Categoria = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipamientos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfilEquipamientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEntrenamientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipamientoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilEquipamientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfilEquipamientos_Equipamientos_EquipamientoId",
                        column: x => x.EquipamientoId,
                        principalTable: "Equipamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerfilEquipamientos_PerfilesEntrenamiento_PerfilEntrenamien~",
                        column: x => x.PerfilEntrenamientoId,
                        principalTable: "PerfilesEntrenamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiasEntrenamientoPreferidos_PerfilEntrenamientoId_Dia",
                table: "DiasEntrenamientoPreferidos",
                columns: new[] { "PerfilEntrenamientoId", "Dia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipamientos_Nombre",
                table: "Equipamientos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfilEquipamientos_EquipamientoId",
                table: "PerfilEquipamientos",
                column: "EquipamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilEquipamientos_PerfilEntrenamientoId_EquipamientoId",
                table: "PerfilEquipamientos",
                columns: new[] { "PerfilEntrenamientoId", "EquipamientoId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PerfilesEntrenamiento_AspNetUsers_UsuarioId",
                table: "PerfilesEntrenamiento",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerfilesEntrenamiento_AspNetUsers_UsuarioId",
                table: "PerfilesEntrenamiento");

            migrationBuilder.DropTable(
                name: "DiasEntrenamientoPreferidos");

            migrationBuilder.DropTable(
                name: "PerfilEquipamientos");

            migrationBuilder.DropTable(
                name: "Equipamientos");

            migrationBuilder.DropColumn(
                name: "DiasEntrenamientoPorSemana",
                table: "PerfilesEntrenamiento");

            migrationBuilder.DropColumn(
                name: "DuracionSesionMinutos",
                table: "PerfilesEntrenamiento");

            migrationBuilder.RenameColumn(
                name: "PesoKg",
                table: "PerfilesEntrenamiento",
                newName: "PesoCorporalKg");

            migrationBuilder.RenameColumn(
                name: "Objetivo",
                table: "PerfilesEntrenamiento",
                newName: "ObjetivoPrincipal");

            migrationBuilder.RenameColumn(
                name: "EntornoEntrenamiento",
                table: "PerfilesEntrenamiento",
                newName: "DuracionSesionDeseadaMinutos");

            migrationBuilder.RenameColumn(
                name: "Edad",
                table: "PerfilesEntrenamiento",
                newName: "DiasDisponiblesSemana");

            migrationBuilder.RenameColumn(
                name: "CreadoEnUtc",
                table: "PerfilesEntrenamiento",
                newName: "FechaUltimaModificacion");

            migrationBuilder.RenameColumn(
                name: "ActualizadoEnUtc",
                table: "PerfilesEntrenamiento",
                newName: "FechaCreacion");

            migrationBuilder.AddColumn<decimal>(
                name: "AlturaCm",
                table: "PerfilesEntrenamiento",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaNacimiento",
                table: "PerfilesEntrenamiento",
                type: "date",
                nullable: true);
        }
    }
}
