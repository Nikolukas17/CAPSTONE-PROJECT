using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Salones;
using OfertaAcademica.Web.ViewModels.Shared;

namespace OfertaAcademica.Web.Controllers;

public class SalonesController(ISalonService salones) : Controller
{
    private const int TamanoPagina = 6;
    private static readonly string[] TiposSalon = ["Laboratorio", "Sala"];

    public IActionResult Index(string? q, int pagina = 1)
    {
        var (items, total) = salones.Buscar(q, Math.Max(1, pagina), TamanoPagina);
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        return View(new SalonListaViewModel
        {
            Items = items.ToList(),
            Paginacion = new PaginacionViewModel
            {
                Pagina = pagina, TamanoPagina = TamanoPagina, TotalResultados = total, Busqueda = q
            }
        });
    }

    // GET /Salones/Detalle/L_001
    public IActionResult Detalle(string id)
    {
        var salon = salones.Obtener(id);
        return salon is null ? NotFound() : View(salon);
    }

    [HttpGet]
    public IActionResult Crear() => View("Formulario", Preparar(new SalonFormViewModel()));

    [HttpGet]
    public IActionResult Editar(string id)
    {
        var salon = salones.Obtener(id);
        if (salon is null) return NotFound();
        return View("Formulario", Preparar(SalonFormViewModel.DesdeEntidad(salon)));
    }

    // POST /Salones/Guardar  (crea o actualiza según EsNuevo)
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Guardar(SalonFormViewModel vm)
    {
        var codigo = vm.Codigo?.Trim().ToUpperInvariant() ?? "";

        if (vm.EsNuevo && codigo.Length > 0 && salones.ExisteCodigo(codigo))
            ModelState.AddModelError(nameof(vm.Codigo), "Ya existe un salón con este código.");

        if (!vm.EsNuevo && salones.Obtener(codigo) is null)
            return NotFound();

        if (!TiposSalon.Contains(vm.Tipo))
            ModelState.AddModelError(nameof(vm.Tipo), "Selecciona el tipo de salón.");

        if (!ModelState.IsValid)
            return View("Formulario", Preparar(vm));

        var guardado = salones.Guardar(vm.ComoEntidad());
        TempData["Exito"] = vm.EsNuevo
            ? $"Creamos el salón {guardado.Nombre}."
            : "Guardamos los cambios del salón.";

        return RedirectToAction(nameof(Detalle), new { id = guardado.Codigo });
    }

    // POST /Salones/Inactivar/L_001
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Inactivar(string id)
    {
        var salon = salones.Obtener(id);
        if (salon is not null && salones.Inactivar(id))
            TempData["Exito"] = $"Inactivamos el salón {salon.Nombre}. Su historial se conserva.";
        else
            TempData["Error"] = "No pudimos inactivar el salón.";

        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------

    private static SalonFormViewModel Preparar(SalonFormViewModel vm)
    {
        vm.Tipos = TiposSalon.Select(t => new SelectListItem(t, t)).ToList();
        return vm;
    }
}
