using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.ViewModels.Shared;

namespace OfertaAcademica.Web.ViewModels.Asignaturas;

public class AsignaturaListaViewModel
{
    public IReadOnlyList<Asignatura> Items { get; init; } = [];
    public PaginacionViewModel Paginacion { get; init; } = new();
}
