using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entrenate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionAdaptationAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SesionesAdaptacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SesionEntrenamientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PolicyVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AdaptationProposalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProposalHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AvailableMinutes = table.Column<int>(type: "integer", nullable: true),
                    EffortPreference = table.Column<int>(type: "integer", nullable: false),
                    Factibilidad = table.Column<int>(type: "integer", nullable: false),
                    FechaConfirmacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesAdaptacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionesAdaptacion_SesionesEntrenamiento_SesionEntrenamient~",
                        column: x => x.SesionEntrenamientoId,
                        principalTable: "SesionesEntrenamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SesionAdaptacionCambios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SesionAdaptacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    TipoCambio = table.Column<int>(type: "integer", nullable: false),
                    Motivo = table.Column<int>(type: "integer", nullable: true),
                    SourceRoutineExerciseId = table.Column<Guid>(type: "uuid", nullable: true),
                    EjercicioOriginalId = table.Column<Guid>(type: "uuid", nullable: true),
                    EjercicioOriginalCodigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EjercicioAdaptadoId = table.Column<Guid>(type: "uuid", nullable: true),
                    EjercicioAdaptadoCodigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SeriesOriginal = table.Column<int>(type: "integer", nullable: true),
                    SeriesAdaptada = table.Column<int>(type: "integer", nullable: true),
                    RirMinimoOriginal = table.Column<int>(type: "integer", nullable: true),
                    RirMaximoOriginal = table.Column<int>(type: "integer", nullable: true),
                    RirMinimoAdaptado = table.Column<int>(type: "integer", nullable: true),
                    RirMaximoAdaptado = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionAdaptacionCambios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionAdaptacionCambios_Ejercicios_EjercicioAdaptadoId",
                        column: x => x.EjercicioAdaptadoId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SesionAdaptacionCambios_Ejercicios_EjercicioOriginalId",
                        column: x => x.EjercicioOriginalId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SesionAdaptacionCambios_SesionesAdaptacion_SesionAdaptacion~",
                        column: x => x.SesionAdaptacionId,
                        principalTable: "SesionesAdaptacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SesionAdaptacionEjerciciosExcluidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SesionAdaptacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EjercicioId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionAdaptacionEjerciciosExcluidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionAdaptacionEjerciciosExcluidos_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SesionAdaptacionEjerciciosExcluidos_SesionesAdaptacion_Sesi~",
                        column: x => x.SesionAdaptacionId,
                        principalTable: "SesionesAdaptacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SesionAdaptacionEquipamientosDisponibles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SesionAdaptacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipamientoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionAdaptacionEquipamientosDisponibles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionAdaptacionEquipamientosDisponibles_Equipamientos_Equi~",
                        column: x => x.EquipamientoId,
                        principalTable: "Equipamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SesionAdaptacionEquipamientosDisponibles_SesionesAdaptacion~",
                        column: x => x.SesionAdaptacionId,
                        principalTable: "SesionesAdaptacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SesionAdaptacionCambios_EjercicioAdaptadoId",
                table: "SesionAdaptacionCambios",
                column: "EjercicioAdaptadoId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionAdaptacionCambios_EjercicioOriginalId",
                table: "SesionAdaptacionCambios",
                column: "EjercicioOriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionAdaptacionCambios_SesionAdaptacionId_Orden",
                table: "SesionAdaptacionCambios",
                columns: new[] { "SesionAdaptacionId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionAdaptacionEjerciciosExcluidos_EjercicioId",
                table: "SesionAdaptacionEjerciciosExcluidos",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionAdaptacionEjerciciosExcluidos_SesionAdaptacionId_Ejer~",
                table: "SesionAdaptacionEjerciciosExcluidos",
                columns: new[] { "SesionAdaptacionId", "EjercicioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionAdaptacionEquipamientosDisponibles_EquipamientoId",
                table: "SesionAdaptacionEquipamientosDisponibles",
                column: "EquipamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionAdaptacionEquipamientosDisponibles_SesionAdaptacionId~",
                table: "SesionAdaptacionEquipamientosDisponibles",
                columns: new[] { "SesionAdaptacionId", "EquipamientoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionesAdaptacion_ProposalHash",
                table: "SesionesAdaptacion",
                column: "ProposalHash");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesAdaptacion_SesionEntrenamientoId",
                table: "SesionesAdaptacion",
                column: "SesionEntrenamientoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionesAdaptacion_UsuarioId_AdaptationProposalId",
                table: "SesionesAdaptacion",
                columns: new[] { "UsuarioId", "AdaptationProposalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SesionAdaptacionCambios");

            migrationBuilder.DropTable(
                name: "SesionAdaptacionEjerciciosExcluidos");

            migrationBuilder.DropTable(
                name: "SesionAdaptacionEquipamientosDisponibles");

            migrationBuilder.DropTable(
                name: "SesionesAdaptacion");
        }
    }
}
