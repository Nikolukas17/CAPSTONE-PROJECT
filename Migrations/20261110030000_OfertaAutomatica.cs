using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using OfertaAcademica.Web.Data;

#nullable disable

namespace OfertaAcademica.Web.Migrations;

public partial class OfertaAutomatica : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Semestre", table: "Asignaturas", type: "TEXT", nullable: true);

        migrationBuilder.CreateTable(
            name: "ProyeccionesOferta",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Nombre = table.Column<string>(type: "TEXT", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                Estado = table.Column<string>(type: "TEXT", nullable: false),
                Mensaje = table.Column<string>(type: "TEXT", nullable: false),
                EsActual = table.Column<bool>(type: "INTEGER", nullable: false),
                TotalGruposSolicitados = table.Column<int>(type: "INTEGER", nullable: false),
                TotalGruposProgramados = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ProyeccionesOferta", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SolicitudesOferta",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ProyeccionOfertaId = table.Column<int>(type: "INTEGER", nullable: false),
                AsignaturaId = table.Column<int>(type: "INTEGER", nullable: false),
                Incluir = table.Column<bool>(type: "INTEGER", nullable: false),
                CantidadGrupos = table.Column<int>(type: "INTEGER", nullable: false),
                Cupo = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SolicitudesOferta", x => x.Id);
                table.ForeignKey("FK_SolicitudesOferta_ProyeccionesOferta_ProyeccionOfertaId", x => x.ProyeccionOfertaId, "ProyeccionesOferta", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_SolicitudesOferta_Asignaturas_AsignaturaId", x => x.AsignaturaId, "Asignaturas", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "RelacionesAsignatura",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                AsignaturaId = table.Column<int>(type: "INTEGER", nullable: false),
                RelacionadaId = table.Column<int>(type: "INTEGER", nullable: false),
                Tipo = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RelacionesAsignatura", x => x.Id);
                table.ForeignKey("FK_RelacionesAsignatura_Asignaturas_AsignaturaId", x => x.AsignaturaId, "Asignaturas", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_RelacionesAsignatura_Asignaturas_RelacionadaId", x => x.RelacionadaId, "Asignaturas", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "GruposOferta",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ProyeccionOfertaId = table.Column<int>(type: "INTEGER", nullable: false),
                AsignaturaId = table.Column<int>(type: "INTEGER", nullable: false),
                ProfesorId = table.Column<int>(type: "INTEGER", nullable: false),
                SalonCodigo = table.Column<string>(type: "TEXT", nullable: false),
                NumeroGrupo = table.Column<int>(type: "INTEGER", nullable: false),
                Cupo = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GruposOferta", x => x.Id);
                table.ForeignKey("FK_GruposOferta_ProyeccionesOferta_ProyeccionOfertaId", x => x.ProyeccionOfertaId, "ProyeccionesOferta", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_GruposOferta_Asignaturas_AsignaturaId", x => x.AsignaturaId, "Asignaturas", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_GruposOferta_Profesores_ProfesorId", x => x.ProfesorId, "Profesores", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_GruposOferta_Salones_SalonCodigo", x => x.SalonCodigo, "Salones", "Codigo", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SesionesOferta",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                GrupoOfertaId = table.Column<int>(type: "INTEGER", nullable: false),
                Dia = table.Column<int>(type: "INTEGER", nullable: false),
                InicioMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                FinMinutos = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SesionesOferta", x => x.Id);
                table.ForeignKey("FK_SesionesOferta_GruposOferta_GrupoOfertaId", x => x.GrupoOfertaId, "GruposOferta", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_SolicitudesOferta_ProyeccionOfertaId", "SolicitudesOferta", "ProyeccionOfertaId");
        migrationBuilder.CreateIndex("IX_SolicitudesOferta_AsignaturaId", "SolicitudesOferta", "AsignaturaId");
        migrationBuilder.CreateIndex("IX_RelacionesAsignatura_AsignaturaId_RelacionadaId_Tipo", "RelacionesAsignatura", new[] { "AsignaturaId", "RelacionadaId", "Tipo" }, unique: true);
        migrationBuilder.CreateIndex("IX_RelacionesAsignatura_RelacionadaId", "RelacionesAsignatura", "RelacionadaId");
        migrationBuilder.CreateIndex("IX_GruposOferta_ProyeccionOfertaId", "GruposOferta", "ProyeccionOfertaId");
        migrationBuilder.CreateIndex("IX_GruposOferta_AsignaturaId", "GruposOferta", "AsignaturaId");
        migrationBuilder.CreateIndex("IX_GruposOferta_ProfesorId", "GruposOferta", "ProfesorId");
        migrationBuilder.CreateIndex("IX_GruposOferta_SalonCodigo", "GruposOferta", "SalonCodigo");
        migrationBuilder.CreateIndex("IX_SesionesOferta_GrupoOfertaId", "SesionesOferta", "GrupoOfertaId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("SesionesOferta");
        migrationBuilder.DropTable("SolicitudesOferta");
        migrationBuilder.DropTable("GruposOferta");
        migrationBuilder.DropTable("RelacionesAsignatura");
        migrationBuilder.DropTable("ProyeccionesOferta");
        migrationBuilder.DropColumn("Semestre", "Asignaturas");
    }
}
