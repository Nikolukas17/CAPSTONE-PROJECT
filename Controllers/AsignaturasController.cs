using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfertaAcademica.Web.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Asignaturas;
using OfertaAcademica.Web.ViewModels.Shared;

namespace OfertaAcademica.Web.Controllers;

[Authorize]
public class AsignaturasController(IAsignaturaService servicio, AppDbContext db, GeneradorOfertaService generador) : Controller
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
        var relaciones = db.RelacionesAsignatura.AsNoTracking().Where(r => r.AsignaturaId == id).ToList();
        vm.PrerrequisitosSeleccionados = relaciones.Where(r => r.Tipo == TipoRelacionAsignatura.Prerrequisito).Select(r => r.RelacionadaId).ToList();
        vm.CorrequisitosSeleccionados = relaciones.Where(r => r.Tipo == TipoRelacionAsignatura.Correquisito).Select(r => r.RelacionadaId).ToList();
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

        vm.ProgramasSeleccionados ??= [];
        vm.PrerrequisitosSeleccionados ??= [];
        vm.CorrequisitosSeleccionados ??= [];
        vm.DisponibilidadesGrupos ??= [];

        if (!string.IsNullOrWhiteSpace(vm.Codigo) && servicio.ExisteCodigo(vm.Codigo, vm.Id))
            ModelState.AddModelError(nameof(vm.Codigo), "Ya existe otra asignatura con este código.");

        var idsRelacionados = vm.PrerrequisitosSeleccionados.Concat(vm.CorrequisitosSeleccionados).Distinct().ToList();
        if (idsRelacionados.Any(x => x == vm.Id || !db.Asignaturas.Any(a => a.Id == x && a.Activa)))
            ModelState.AddModelError(string.Empty, "Verifica las asignaturas seleccionadas como prerrequisito o correquisito.");
        if (vm.PrerrequisitosSeleccionados.Intersect(vm.CorrequisitosSeleccionados).Any())
            ModelState.AddModelError(string.Empty, "Una asignatura no puede ser prerrequisito y correquisito de la misma materia al mismo tiempo.");

        if (vm.ProgramasSeleccionados.Count == 0 ||
            vm.ProgramasSeleccionados.Any(c => !db.Programas.Any(p => p.Codigo == c)))
            ModelState.AddModelError(nameof(vm.ProgramasSeleccionados), "Selecciona uno o más programas válidos.");

        if (vm.CantidadGrupos < 1 || vm.CantidadGrupos > 30)
            ModelState.AddModelError(nameof(vm.CantidadGrupos), "La cantidad de grupos debe estar entre 1 y 30.");

        if (!ModelState.IsValid)
        {
            vm.ModoEdicion = true;
            return View("Detalle", PrepararListas(vm));
        }

        var entidad = vm.ComoEntidad();
        if (existente is not null)
            ConservarFranjasOcupadas(existente, entidad);

        var guardada = servicio.Guardar(entidad);
        db.RelacionesAsignatura.RemoveRange(db.RelacionesAsignatura.Where(r => r.AsignaturaId == guardada.Id));
        foreach (var idRelacionado in vm.PrerrequisitosSeleccionados.Distinct().Where(x => x != guardada.Id))
            db.RelacionesAsignatura.Add(new RelacionAsignatura { AsignaturaId = guardada.Id, RelacionadaId = idRelacionado, Tipo = TipoRelacionAsignatura.Prerrequisito });
        foreach (var idRelacionado in vm.CorrequisitosSeleccionados.Distinct().Where(x => x != guardada.Id))
            db.RelacionesAsignatura.Add(new RelacionAsignatura { AsignaturaId = guardada.Id, RelacionadaId = idRelacionado, Tipo = TipoRelacionAsignatura.Correquisito });
        db.SaveChanges();
        generador.RecalcularProyeccionActual();
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
        {
            generador.RecalcularProyeccionActual();
            TempData["Exito"] = $"Inactivamos {asignatura.Nombre}. Su historial se conserva.";
        }
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

        vm.AsignaturasRelacionables = db.Asignaturas.AsNoTracking()
            .Where(a => a.Activa && a.Id != vm.Id)
            .OrderBy(a => a.Codigo)
            .ToList()
            .GroupBy(a => a.Codigo, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(a => a.Id).First())
            .Select(a => new SelectListItem($"{a.Codigo} · {a.Nombre}", a.Id.ToString()))
            .ToList();

        vm.Tipologias = Enum.GetValues<Tipologia>()
            .Select(t => new SelectListItem(t.ToString().ToUpperInvariant(), t.ToString()))
            .ToList();

        if (vm.Disponibilidad.Count != DisponibilidadSemanal.Total)
            vm.Disponibilidad = DisponibilidadSemanal.Base();
        if (vm.DisponibilidadesGrupos.Count != vm.CantidadGrupos)
            vm.DisponibilidadesGrupos = Enumerable.Range(0, Math.Clamp(vm.CantidadGrupos, 1, 30))
                .Select(i => i < vm.DisponibilidadesGrupos.Count ? vm.DisponibilidadesGrupos[i] : vm.Disponibilidad.ToList()).ToList();
        foreach (var grupo in vm.DisponibilidadesGrupos)
            if (grupo.Count != DisponibilidadSemanal.Total)
                grupo.Clear();

        return vm;
    }

    /// <summary>
    /// Las franjas "Ocupado" las define la oferta académica, no el formulario:
    /// se conservan las que ya estaban y se ignoran las que llegan nuevas.
    /// </summary>
    private static void ConservarFranjasOcupadas(Asignatura original, Asignatura nueva)
    {
        static void Fusionar(List<EstadoFranja> anterior, List<EstadoFranja> actual)
        {
            for (var i = 0; i < DisponibilidadSemanal.Total; i++)
            {
                if (i < anterior.Count && anterior[i] == EstadoFranja.Ocupado)
                    actual[i] = EstadoFranja.Ocupado;
                else if (actual[i] == EstadoFranja.Ocupado)
                    actual[i] = EstadoFranja.NoDisponible;
            }
        }

        Fusionar(original.Disponibilidad, nueva.Disponibilidad);
        try
        {
            var anteriores = System.Text.Json.JsonSerializer.Deserialize<List<List<EstadoFranja>>>(original.DisponibilidadesGruposJson)
                ?? Enumerable.Range(0, Math.Max(1, original.CantidadGrupos)).Select(_ => original.Disponibilidad.ToList()).ToList();
            var actuales = System.Text.Json.JsonSerializer.Deserialize<List<List<EstadoFranja>>>(nueva.DisponibilidadesGruposJson) ?? [];
            for (var g = 0; g < actuales.Count; g++)
                if (g < anteriores.Count) Fusionar(anteriores[g], actuales[g]);
            nueva.DisponibilidadesGruposJson = System.Text.Json.JsonSerializer.Serialize(actuales);
        }
        catch { }
    }
}
