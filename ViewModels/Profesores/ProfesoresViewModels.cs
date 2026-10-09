using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.ViewModels.Shared;

namespace OfertaAcademica.Web.ViewModels.Profesores;

public class ProfesorListaViewModel
{
    public IReadOnlyList<Profesor> Items { get; init; } = [];
    public PaginacionViewModel Paginacion { get; init; } = new();
}
