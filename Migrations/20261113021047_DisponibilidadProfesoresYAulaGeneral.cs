using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfertaAcademica.Web.Migrations
{
    /// <inheritdoc />
    public partial class DisponibilidadProfesoresYAulaGeneral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisponibilidadActualizadaUtc",
                table: "Profesores",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenDisponibilidadExpiraUtc",
                table: "Profesores",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenDisponibilidadHash",
                table: "Profesores",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SalonCodigo",
                table: "GruposOferta",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Profesores_TokenDisponibilidadHash",
                table: "Profesores",
                column: "TokenDisponibilidadHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profesores_TokenDisponibilidadHash",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "DisponibilidadActualizadaUtc",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "TokenDisponibilidadExpiraUtc",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "TokenDisponibilidadHash",
                table: "Profesores");

            migrationBuilder.AlterColumn<string>(
                name: "SalonCodigo",
                table: "GruposOferta",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
