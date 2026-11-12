using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OfertaAcademica.Web.Data;

#nullable disable
namespace OfertaAcademica.Web.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261112010000_UsuariosYPlanificacionPregrado")]
public partial class UsuariosYPlanificacionPregrado : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UsuariosAplicacion",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                NombreCompleto = table.Column<string>(type: "TEXT", nullable: false),
                Cargo = table.Column<string>(type: "TEXT", nullable: false),
                Correo = table.Column<string>(type: "TEXT", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                FechaRegistroUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Activo = table.Column<bool>(type: "INTEGER", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_UsuariosAplicacion", x => x.Id));

        migrationBuilder.CreateTable(
            name: "PlanificacionPregrado",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                NombreAsignatura = table.Column<string>(type: "TEXT", nullable: false),
                DatosJson = table.Column<string>(type: "TEXT", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_PlanificacionPregrado", x => x.Id));

        migrationBuilder.CreateTable(
            name: "TokensRecuperacion",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                UsuarioAplicacionId = table.Column<int>(type: "INTEGER", nullable: false),
                TokenHash = table.Column<string>(type: "TEXT", nullable: false),
                ExpiraUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Usado = table.Column<bool>(type: "INTEGER", nullable: false)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_TokensRecuperacion", x => x.Id);
                table.ForeignKey("FK_TokensRecuperacion_UsuariosAplicacion_UsuarioAplicacionId", x => x.UsuarioAplicacionId, "UsuariosAplicacion", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_UsuariosAplicacion_Correo", "UsuariosAplicacion", "Correo", unique: true);
        migrationBuilder.CreateIndex("IX_PlanificacionPregrado_NombreAsignatura", "PlanificacionPregrado", "NombreAsignatura");
        migrationBuilder.CreateIndex("IX_TokensRecuperacion_TokenHash", "TokensRecuperacion", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_TokensRecuperacion_UsuarioAplicacionId", "TokensRecuperacion", "UsuarioAplicacionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TokensRecuperacion");
        migrationBuilder.DropTable("PlanificacionPregrado");
        migrationBuilder.DropTable("UsuariosAplicacion");
    }
}
