using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Services;

public interface IProfesorService
{
    (IReadOnlyList<Profesor> Items, int Total) Buscar(string? texto, int pagina, int tamanoPagina);
    Profesor? Obtener(int id);
    bool ExisteCorreo(string correo, int idExcluido = 0);
    Profesor Guardar(Profesor profesor);
    bool Inactivar(int id);
}
