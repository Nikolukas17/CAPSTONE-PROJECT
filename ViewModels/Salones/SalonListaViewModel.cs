using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.ViewModels.Shared;

namespace OfertaAcademica.Web.ViewModels.Salones;

public class SalonListaViewModel
{
    public IReadOnlyList<Salon> Items { get; init; } = [];
    public PaginacionViewModel Paginacion { get; init; } = new();
}
