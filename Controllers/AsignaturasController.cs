using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Asignaturas;
using OfertaAcademica.Web.ViewModels.Shared;

namespace OfertaAcademica.Web.Controllers;

public class AsignaturasController(IAsignaturaService servicio) : Controller
{
    private const int TamanoPagina = 10;

    // GET /Asignaturas?q=algebra&pagina=2
    public IActionResult Index(string? q, int pagina = 1)
    {
        pagina = Math.Max(1, pagina);
        var (items, total) = servicio.Buscar(q, pagina, TamanoPagina);

        // Si la página pedida ya no existe (p. ej. tras inactivar la última fila), vuelve a la última.
        if (items.Count == 0 && total > 0)
        {
            pagina = (int)Math.Ceiling(total / (double)TamanoPagina);
            (items, total) = servicio.Buscar(q, pagina, TamanoPagina);
        }

        return View(new AsignaturaListaViewModel
        {
            Items = items,
            Paginacion = new PaginacionViewModel
            {
                Pagina = pagina,
                TamanoPagina = TamanoPagina,
                TotalResultados = total,
                Busqueda = q
            }
        });
    }

    // GET /Asignaturas/Detalle/3        -> solo lectura
    // GET /Asignaturas/Detalle/3?editar=true -> abre directamente en modo edición
    public IActionResult Detalle(int id, bool editar = false)
    {
        var asignatura = servicio.Obtener(id);
        if (asignatura is null)
        {
            TempData["Error"] = "No encontramos esa asignatura. Puede que ya esté inactiva.";
            return RedirectToAction(nameof(Index));
        }

        var vm = AsignaturaFormViewModel.Desde(asignatura);
        vm.ModoEdicion = editar;
        return View(PrepararListas(vm));
    }

    // GET /Asignaturas/Nueva
    public IActionResult Nueva()
    {
        var vm = new AsignaturaFormViewModel { ModoEdicion = true };
        return View("Detalle", PrepararListas(vm));
    }

    // POST /Asignaturas/Guardar  (crea o actualiza según Id)
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Guardar(AsignaturaFormViewModel vm)
    {
        Asignatura? existente = null;
        if (vm.Id != 0)
        {
            existente = servicio.Obtener(vm.Id);
            if (existente is null) return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(vm.Codigo) && servicio.ExisteCodigo(vm.Codigo, vm.Id))
            ModelState.AddModelError(nameof(vm.Codigo), "Ya existe una asignatura con este código.");

        if (!ModelState.IsValid)
        {
            vm.ModoEdicion = true;
            return View("Detalle", PrepararListas(vm));
        }

        var entidad = vm.ComoEntidad();
        if (existente is not null)
            ConservarFranjasOcupadas(existente, entidad);

        var guardada = servicio.Guardar(entidad);
        TempData["Exito"] = vm.EsNueva
            ? $"Creamos la asignatura {guardada.Nombre}."
            : "Guardamos los cambios de la asignatura.";

        return RedirectToAction(nameof(Detalle), new { id = guardada.Id });
    }

    // POST /Asignaturas/Inactivar/3
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Inactivar(int id)
    {
        var asignatura = servicio.Obtener(id);
        if (asignatura is not null && servicio.Inactivar(id))
            TempData["Exito"] = $"Inactivamos {asignatura.Nombre}. Su historial se conserva.";
        else
            TempData["Error"] = "No pudimos inactivar la asignatura.";

        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------

    private AsignaturaFormViewModel PrepararListas(AsignaturaFormViewModel vm)
    {
        vm.Programas = servicio.ObtenerProgramas()
            .Select(p => new SelectListItem($"{p.Codigo} · {p.Nombre}", p.Codigo))
            .ToList();

        vm.Tipologias = Enum.GetValues<Tipologia>()
            .Select(t => new SelectListItem(t.ToString().ToUpperInvariant(), t.ToString()))
            .ToList();

        if (vm.Disponibilidad.Count != DisponibilidadSemanal.Total)
            vm.Disponibilidad = DisponibilidadSemanal.Base();

        return vm;
    }

    /// <summary>
    /// Las franjas "Ocupado" las define la oferta académica, no el formulario:
    /// se conservan las que ya estaban y se ignoran las que llegan nuevas.
    /// </summary>
    private static void ConservarFranjasOcupadas(Asignatura original, Asignatura nueva)
    {
        for (var i = 0; i < DisponibilidadSemanal.Total; i++)
        {
            if (original.Disponibilidad[i] == EstadoFranja.Ocupado)
                nueva.Disponibilidad[i] = EstadoFranja.Ocupado;
            else if (nueva.Disponibilidad[i] == EstadoFranja.Ocupado)
                nueva.Disponibilidad[i] = EstadoFranja.NoDisponible;
        }
    }
}
