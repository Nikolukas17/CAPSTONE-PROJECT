using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Services;

public interface ISalonService
{
    (IReadOnlyList<Salon> Items, int Total) Buscar(string? texto, int pagina, int tamanoPagina);
    Salon? Obtener(string codigo);
    bool ExisteCodigo(string codigo);
    Salon Guardar(Salon salon);
    bool Inactivar(string codigo);
}
