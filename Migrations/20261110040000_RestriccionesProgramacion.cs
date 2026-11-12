using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OfertaAcademica.Web.Data;

#nullable disable

namespace OfertaAcademica.Web.Migrations;

[Migration("20261110040000_RestriccionesProgramacion")]
public partial class RestriccionesProgramacion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("Disponibilidad", "Salones", type: "TEXT", nullable: false,
            defaultValue: "[]");
        migrationBuilder.AddColumn<string>("TipoSalonRequerido", "Asignaturas", type: "TEXT", nullable: false,
            defaultValue: "Cualquiera");
        migrationBuilder.AddColumn<string>("TipoAsignatura", "Asignaturas", type: "TEXT", nullable: false,
            defaultValue: "Teórica");
        migrationBuilder.AddColumn<int>("ComputadoresRequeridos", "Asignaturas", type: "INTEGER", nullable: false,
            defaultValue: 0);
        migrationBuilder.AddColumn<string>("SalonesPermitidosCsv", "Asignaturas", type: "TEXT", nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<int>("CursosMinimos", "Profesores", type: "INTEGER", nullable: false,
            defaultValue: 0);
        migrationBuilder.AddColumn<int>("CursosMaximos", "Profesores", type: "INTEGER", nullable: false,
            defaultValue: 99);
        migrationBuilder.AddColumn<int>("HorasMinimas", "Profesores", type: "INTEGER", nullable: false,
            defaultValue: 0);
        migrationBuilder.AddColumn<int>("HorasMaximas", "Profesores", type: "INTEGER", nullable: false,
            defaultValue: 200);
        // Al convertir la disponibilidad antigua (bloques de 2 horas), se reinicia a la
        // disponibilidad base de 30 minutos para no interpretar índices antiguos incorrectamente.
        var baseSlots = new System.Collections.Generic.List<int>();
        for (var slot = 0; slot < 144; slot++)
        {
            var dia = slot % 6;
            var franja = slot / 6;
            baseSlots.Add(dia < 5 ? 0 : 2);
        }
        var json = System.Text.Json.JsonSerializer.Serialize(baseSlots);
        migrationBuilder.Sql($"UPDATE Asignaturas SET Disponibilidad = '{json}'");
        migrationBuilder.Sql($"UPDATE Profesores SET Disponibilidad = '{json}'");
        migrationBuilder.Sql($"UPDATE Salones SET Disponibilidad = '{json}'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("Disponibilidad", "Salones");
        migrationBuilder.DropColumn("TipoSalonRequerido", "Asignaturas");
        migrationBuilder.DropColumn("TipoAsignatura", "Asignaturas");
        migrationBuilder.DropColumn("ComputadoresRequeridos", "Asignaturas");
        migrationBuilder.DropColumn("SalonesPermitidosCsv", "Asignaturas");
        migrationBuilder.DropColumn("CursosMinimos", "Profesores");
        migrationBuilder.DropColumn("CursosMaximos", "Profesores");
        migrationBuilder.DropColumn("HorasMinimas", "Profesores");
        migrationBuilder.DropColumn("HorasMaximas", "Profesores");
    }
}
