using Microsoft.AspNetCore.Mvc;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Profesores;
using OfertaAcademica.Web.ViewModels.Shared;

namespace OfertaAcademica.Web.Controllers;

public class ProfesoresController : Controller
{
    private const int TamanoPagina = 6;

    public IActionResult Index(string? q, int pagina = 1)
    {
        var consulta = DatosDemo.Profesores.Where(p => p.Activo);
        if (!string.IsNullOrWhiteSpace(q))
            consulta = consulta.Where(p =>
                p.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Correo.Contains(q, StringComparison.OrdinalIgnoreCase));

        var lista = consulta.ToList();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(lista.Count / (double)TamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        return View(new ProfesorListaViewModel
        {
            Items = lista.Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina).ToList(),
            Paginacion = new PaginacionViewModel
            {
                Pagina = pagina, TamanoPagina = TamanoPagina, TotalResultados = lista.Count, Busqueda = q
            }
        });
    }

    public IActionResult Detalle(int id)
    {
        var profesor = DatosDemo.Profesores.FirstOrDefault(p => p.Id == id);
        return profesor is null ? RedirectToAction(nameof(Index)) : View(profesor);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Inactivar(int id)
    {
        var profesor = DatosDemo.Profesores.FirstOrDefault(p => p.Id == id);
        if (profesor is not null)
        {
            profesor.Activo = false;
            TempData["Exito"] = $"Inactivamos a {profesor.Nombre}. Su historial se conserva.";
        }
        return RedirectToAction(nameof(Index));
    }
}
