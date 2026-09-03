using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entrenate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ejercicios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GrupoMuscularPrincipal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Equipamiento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WorkoutGuideId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ImagenPrincipalUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ejercicios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfilesEntrenamiento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    NivelExperiencia = table.Column<int>(type: "integer", nullable: false),
                    Sexo = table.Column<int>(type: "integer", nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    AlturaCm = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    PesoCorporalKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    ObjetivoPrincipal = table.Column<int>(type: "integer", nullable: false),
                    DiasDisponiblesSemana = table.Column<int>(type: "integer", nullable: false),
                    DuracionSesionDeseadaMinutos = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilesEntrenamiento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rutinas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rutinas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiasRutina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RutinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiasRutina", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiasRutina_Rutinas_RutinaId",
                        column: x => x.RutinaId,
                        principalTable: "Rutinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EjerciciosRutina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaRutinaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EjercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CantidadSeries = table.Column<int>(type: "integer", nullable: false),
                    RepeticionesMinimas = table.Column<int>(type: "integer", nullable: false),
                    RepeticionesMaximas = table.Column<int>(type: "integer", nullable: false),
                    RirObjetivoMinimo = table.Column<int>(type: "integer", nullable: false),
                    RirObjetivoMaximo = table.Column<int>(type: "integer", nullable: false),
                    DescansoSegundos = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Notas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjerciciosRutina", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EjerciciosRutina_DiasRutina_DiaRutinaId",
                        column: x => x.DiaRutinaId,
                        principalTable: "DiasRutina",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EjerciciosRutina_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SesionesEntrenamiento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DiaRutinaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoraInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoraFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Notas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesEntrenamiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionesEntrenamiento_DiasRutina_DiaRutinaId",
                        column: x => x.DiaRutinaId,
                        principalTable: "DiasRutina",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EjerciciosSesion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SesionEntrenamientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    EjercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    SeriesObjetivo = table.Column<int>(type: "integer", nullable: false),
                    RepeticionesMinimasObjetivo = table.Column<int>(type: "integer", nullable: false),
                    RepeticionesMaximasObjetivo = table.Column<int>(type: "integer", nullable: false),
                    RirObjetivoMinimo = table.Column<int>(type: "integer", nullable: false),
                    RirObjetivoMaximo = table.Column<int>(type: "integer", nullable: false),
                    DescansoObjetivoSegundos = table.Column<int>(type: "integer", nullable: false),
                    Notas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjerciciosSesion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EjerciciosSesion_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EjerciciosSesion_SesionesEntrenamiento_SesionEntrenamientoId",
                        column: x => x.SesionEntrenamientoId,
                        principalTable: "SesionesEntrenamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeriesEntrenamiento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EjercicioSesionId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroSerie = table.Column<int>(type: "integer", nullable: false),
                    Peso = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Repeticiones = table.Column<int>(type: "integer", nullable: false),
                    Rir = table.Column<int>(type: "integer", nullable: true),
                    Completada = table.Column<bool>(type: "boolean", nullable: false),
                    FechaHoraRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesEntrenamiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesEntrenamiento_EjerciciosSesion_EjercicioSesionId",
                        column: x => x.EjercicioSesionId,
                        principalTable: "EjerciciosSesion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiasRutina_RutinaId_Orden",
                table: "DiasRutina",
                columns: new[] { "RutinaId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ejercicios_WorkoutGuideId",
                table: "Ejercicios",
                column: "WorkoutGuideId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EjerciciosRutina_DiaRutinaId_Orden",
                table: "EjerciciosRutina",
                columns: new[] { "DiaRutinaId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EjerciciosRutina_EjercicioId",
                table: "EjerciciosRutina",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_EjerciciosSesion_EjercicioId",
                table: "EjerciciosSesion",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_EjerciciosSesion_SesionEntrenamientoId_Orden",
                table: "EjerciciosSesion",
                columns: new[] { "SesionEntrenamientoId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesEntrenamiento_UsuarioId",
                table: "PerfilesEntrenamiento",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rutinas_UsuarioId",
                table: "Rutinas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesEntrenamiento_EjercicioSesionId_NumeroSerie",
                table: "SeriesEntrenamiento",
                columns: new[] { "EjercicioSesionId", "NumeroSerie" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionesEntrenamiento_DiaRutinaId",
                table: "SesionesEntrenamiento",
                column: "DiaRutinaId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesEntrenamiento_UsuarioId_Fecha",
                table: "SesionesEntrenamiento",
                columns: new[] { "UsuarioId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EjerciciosRutina");

            migrationBuilder.DropTable(
                name: "PerfilesEntrenamiento");

            migrationBuilder.DropTable(
                name: "SeriesEntrenamiento");

            migrationBuilder.DropTable(
                name: "EjerciciosSesion");

            migrationBuilder.DropTable(
                name: "Ejercicios");

            migrationBuilder.DropTable(
                name: "SesionesEntrenamiento");

            migrationBuilder.DropTable(
                name: "DiasRutina");

            migrationBuilder.DropTable(
                name: "Rutinas");
        }
    }
}
