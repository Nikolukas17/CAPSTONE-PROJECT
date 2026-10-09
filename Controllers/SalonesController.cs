using Microsoft.AspNetCore.Mvc;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Salones;
using OfertaAcademica.Web.ViewModels.Shared;

namespace OfertaAcademica.Web.Controllers;

public class SalonesController : Controller
{
    private const int TamanoPagina = 6;

    public IActionResult Index(string? q, int pagina = 1)
    {
        var consulta = DatosDemo.Salones.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
            consulta = consulta.Where(s =>
                s.Codigo.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase));

        var lista = consulta.ToList();
        var paginacion = new PaginacionViewModel
        {
            TamanoPagina = TamanoPagina, TotalResultados = lista.Count, Busqueda = q, Pagina = 1
        };
        pagina = Math.Clamp(pagina, 1, paginacion.TotalPaginas);

        return View(new SalonListaViewModel
        {
            Items = lista.Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina).ToList(),
            Paginacion = new PaginacionViewModel
            {
                Pagina = pagina, TamanoPagina = TamanoPagina, TotalResultados = lista.Count, Busqueda = q
            }
        });
    }
}
