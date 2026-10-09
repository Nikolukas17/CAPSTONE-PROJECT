using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Services;

/// <summary>
/// Contrato de acceso a asignaturas. Hoy lo implementa <see cref="AsignaturaServiceEnMemoria"/>;
/// cuando llegue la base de datos solo hay que crear otra implementación (EF Core, Dapper, etc.)
/// y cambiar el registro en Program.cs. Los controladores y vistas no se tocan.
/// </summary>
public interface IAsignaturaService
{
    (IReadOnlyList<Asignatura> Items, int Total) Buscar(string? texto, int pagina, int tamanoPagina);
    Asignatura? Obtener(int id);
    bool ExisteCodigo(string codigo, int idExcluido = 0);
    Asignatura Guardar(Asignatura asignatura);
    bool Inactivar(int id);
    IReadOnlyList<Programa> ObtenerProgramas();
}
