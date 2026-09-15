using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entrenate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeExerciseEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Equipamiento",
                table: "Ejercicios");

            migrationBuilder.CreateTable(
                name: "EjercicioEquipamientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EjercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipamientoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjercicioEquipamientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EjercicioEquipamientos_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EjercicioEquipamientos_Equipamientos_EquipamientoId",
                        column: x => x.EquipamientoId,
                        principalTable: "Equipamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EjercicioEquipamientos_EjercicioId_EquipamientoId",
                table: "EjercicioEquipamientos",
                columns: new[] { "EjercicioId", "EquipamientoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EjercicioEquipamientos_EquipamientoId",
                table: "EjercicioEquipamientos",
                column: "EquipamientoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EjercicioEquipamientos");

            migrationBuilder.AddColumn<string>(
                name: "Equipamiento",
                table: "Ejercicios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
