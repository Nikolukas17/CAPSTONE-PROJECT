using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OfertaAcademica.Web.Data;

namespace OfertaAcademica.Web.Migrations;

[Migration("20261110050000_AsignaturasCompartidasYGrupos")]
public partial class AsignaturasCompartidasYGrupos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProgramasCodigosCsv", table: "Asignaturas", type: "TEXT",
            nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<int>(
            name: "CantidadGrupos", table: "Asignaturas", type: "INTEGER",
            nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<string>(
            name: "DisponibilidadesGruposJson", table: "Asignaturas", type: "TEXT",
            nullable: false, defaultValue: "");
        migrationBuilder.Sql("UPDATE Asignaturas SET ProgramasCodigosCsv = ProgramaCodigo WHERE ProgramasCodigosCsv = '';");
        migrationBuilder.Sql(@"UPDATE Asignaturas
            SET ProgramasCodigosCsv = (
                SELECT group_concat(DISTINCT ProgramaCodigo)
                FROM Asignaturas AS relacionadas
                WHERE relacionadas.Codigo = Asignaturas.Codigo
            )
            WHERE Id = (SELECT MIN(Id) FROM Asignaturas AS canon WHERE canon.Codigo = Asignaturas.Codigo);");
        migrationBuilder.DropColumn(name: "ComputadoresRequeridos", table: "Asignaturas");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ComputadoresRequeridos", table: "Asignaturas", type: "INTEGER",
            nullable: false, defaultValue: 0);
        migrationBuilder.DropColumn(name: "DisponibilidadesGruposJson", table: "Asignaturas");
        migrationBuilder.DropColumn(name: "CantidadGrupos", table: "Asignaturas");
        migrationBuilder.DropColumn(name: "ProgramasCodigosCsv", table: "Asignaturas");
    }
}
