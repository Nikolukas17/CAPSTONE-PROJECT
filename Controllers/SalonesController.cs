using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfertaAcademica.Web.Data;
using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Salones;
using OfertaAcademica.Web.ViewModels.Shared;

namespace OfertaAcademica.Web.Controllers;

[Authorize]
public class SalonesController(ISalonService salones, GeneradorOfertaService generador, AppDbContext db) : Controller
{
    private const int TamanoPagina = 6;
    private static readonly string[] TiposSalon = ["LABORATORIO", "SALAS", "CONVENCIONAL", "CÓMPUTO"];

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
        if (salon is null) return NotFound();
        var reservas = ObtenerReservasSalon(id);
        foreach (var reserva in reservas)
            for (var minuto = reserva.Inicio; minuto < reserva.Fin; minuto += 30)
            {
                var franja = (minuto - 7 * 60) / 30;
                if (franja is >= 0 and < 24)
                    salon.Disponibilidad[DisponibilidadSemanal.Indice(franja, (int)reserva.Dia - 1)] = EstadoFranja.Ocupado;
            }
        return View(salon);
    }

    [HttpGet]
    public IActionResult Crear() => View("Formulario", Preparar(new SalonFormViewModel()));

    [HttpGet]
    public IActionResult Editar(string id)
    {
        var salon = salones.Obtener(id);
        if (salon is null) return NotFound();
        var vm = SalonFormViewModel.DesdeEntidad(salon);
        var reservas = ObtenerReservasSalon(id);
        foreach (var reserva in reservas)
            for (var minuto = reserva.Inicio; minuto < reserva.Fin; minuto += 30)
            {
                var franja = (minuto - 7 * 60) / 30;
                if (franja is >= 0 and < 24)
                    vm.Disponibilidad[DisponibilidadSemanal.Indice(franja, (int)reserva.Dia - 1)] = EstadoFranja.Ocupado;
            }
        return View("Formulario", Preparar(vm));
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

        if (!vm.EsNuevo)
        {
            var reservas = ObtenerReservasSalon(codigo);
            foreach (var reserva in reservas)
            {
                if (vm.Capacidad < reserva.Cupo)
                    ModelState.AddModelError(nameof(vm.Capacidad), $"No puedes reducir la capacidad por debajo de {reserva.Cupo}: existe una reserva en una proyección.");
                var salonActual = salones.Obtener(codigo);
                if (salonActual is not null && !string.Equals(vm.Tipo, salonActual.Tipo, StringComparison.OrdinalIgnoreCase))
                    ModelState.AddModelError(nameof(vm.Tipo), "No puedes cambiar el tipo de un salón con reservas en proyecciones.");
                if (salonActual is not null && !string.Equals(vm.Nombre.Trim(), salonActual.Nombre, StringComparison.OrdinalIgnoreCase))
                    ModelState.AddModelError(nameof(vm.Nombre), "No puedes cambiar el nombre de un salón con reservas en proyecciones.");
                for (var minuto = reserva.Inicio; minuto < reserva.Fin; minuto += 30)
                {
                    var franja = (minuto - 7 * 60) / 30;
                    var indice = DisponibilidadSemanal.Indice(franja, (int)reserva.Dia - 1);
                    if (franja < 0 || franja >= 24 || indice >= vm.Disponibilidad.Count ||
                        vm.Disponibilidad[indice] == EstadoFranja.NoDisponible)
                        ModelState.AddModelError(nameof(vm.Disponibilidad), "La disponibilidad no puede bloquear franjas que ya tienen reservas en proyecciones.");
                }
            }
        }

        if (!ModelState.IsValid)
            return View("Formulario", Preparar(vm));

        var guardado = salones.Guardar(vm.ComoEntidad());
        generador.RecalcularProyeccionActual();
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
        {
            generador.RecalcularProyeccionActual();
            TempData["Exito"] = $"Inactivamos el salón {salon.Nombre}. Su historial se conserva.";
        }
        else
            TempData["Error"] = "No pudimos inactivar el salón.";

        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------

    // SQLite no soporta SQL APPLY. Cargamos primero los grupos y sus sesiones
    // y hacemos el aplanado en memoria para evitar una traducción LINQ incompatible.
    private List<ReservaSalon> ObtenerReservasSalon(string codigo)
    {
        var grupos = db.GruposOferta.AsNoTracking()
            .Where(g => g.SalonCodigo == codigo)
            .Include(g => g.Sesiones)
            .ToList();

        return grupos
            .SelectMany(g => g.Sesiones.Select(s => new ReservaSalon(
                g.Cupo, s.InicioMinutos, s.FinMinutos, s.Dia)))
            .ToList();
    }

    private sealed record ReservaSalon(int Cupo, int Inicio, int Fin, DayOfWeek Dia);

    private static SalonFormViewModel Preparar(SalonFormViewModel vm)
    {
        vm.Tipos = TiposSalon.Select(t => new SelectListItem(t, t)).ToList();
        return vm;
    }
}
