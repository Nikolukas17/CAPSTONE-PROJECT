using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfertaAcademica.Web.Data;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Oferta;

namespace OfertaAcademica.Web.Controllers;

[Authorize]
public class OfertaAcademicaController(AppDbContext db, GeneradorOfertaService generador, ExcelOfertaService excel) : Controller
{
    private static readonly DateTime InicioSemestre = new(2026, 8, 3);
    private const int TotalSemanas = 16;

    public IActionResult Index(int? semana, int? id)
    {
        var consulta = db.ProyeccionesOferta.AsNoTracking()
            .Include(p => p.Grupos).ThenInclude(g => g.Asignatura)
            .Include(p => p.Grupos).ThenInclude(g => g.Profesor)
            .Include(p => p.Grupos).ThenInclude(g => g.Salon)
            .Include(p => p.Grupos).ThenInclude(g => g.Sesiones);

        var proyeccion = id.HasValue
            ? consulta.FirstOrDefault(p => p.Id == id.Value)
            : consulta.OrderByDescending(p => p.FechaCreacion).FirstOrDefault();

        var numeroSemana = Math.Clamp(semana ?? 1, 1, TotalSemanas);
        var vm = new OfertaAcademicaViewModel
        {
            Semana = numeroSemana,
            TotalSemanas = TotalSemanas,
            InicioSemana = InicioSemestre.AddDays((numeroSemana - 1) * 7),
            ProyeccionId = proyeccion?.Id,
            NombreProyeccion = proyeccion?.Nombre ?? "",
            EstadoProyeccion = proyeccion?.Estado ?? "Sin proyección",
            MensajeProyeccion = proyeccion?.Mensaje ?? "Aún no has generado una proyección. Crea una para visualizar los horarios.",
            TotalGruposProgramados = proyeccion?.TotalGruposProgramados ?? 0,
            TotalGruposSolicitados = proyeccion?.TotalGruposSolicitados ?? 0,
            Historial = db.ProyeccionesOferta.AsNoTracking().OrderByDescending(p => p.FechaCreacion)
                .Select(p => new ProyeccionResumenVM { Id = p.Id, Nombre = p.Nombre, FechaCreacion = p.FechaCreacion, Estado = p.Estado }).ToList(),
            Sesiones = proyeccion?.Grupos
                .Where(g => g.Asignatura != null && g.Profesor != null && g.Salon != null)
                .SelectMany((g, indice) => g.Sesiones.Select(s => new SesionClaseVM
                {
                    Id = s.Id,
                    AsignaturaId = g.AsignaturaId,
                    Codigo = g.Asignatura!.Codigo,
                    Nombre = g.Asignatura.Nombre,
                    Profesor = g.Profesor!.Nombre,
                    Salon = g.Salon!.Nombre,
                    Programa = g.Asignatura.ProgramasCodigosCsv.Length > 0 ? g.Asignatura.ProgramasCodigosCsv : g.Asignatura.ProgramaCodigo,
                    Grupo = g.NumeroGrupo.ToString(),
                    Cupo = g.Cupo,
                    Dia = s.Dia,
                    Inicio = TimeSpan.FromMinutes(s.InicioMinutos),
                    Fin = TimeSpan.FromMinutes(s.FinMinutos),
                    Color = new[] { "rojo", "verde", "azul", "amarillo", "rosa", "gris" }[indice % 6]
                })).ToList() ?? []
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Generar() => View(PrepararGeneracion(new GenerarOfertaViewModel()));

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Generar(GenerarOfertaViewModel vm)
    {
        AplicarPlanificacionPregrado(vm);
        var seleccionadas = vm.Asignaturas.Where(a => a.Incluir).ToList();
        if (seleccionadas.Count == 0)
            ModelState.AddModelError(nameof(vm.Asignaturas), "Selecciona al menos una asignatura.");

        if (seleccionadas.Any(a => a.CantidadGrupos < 1 || a.CantidadGrupos > 30 || a.Cupo < 1 || a.Cupo > 500))
            ModelState.AddModelError(nameof(vm.Asignaturas), "Verifica la cantidad de grupos y los cupos solicitados.");

        if (!ModelState.IsValid)
            return View(PrepararGeneracion(vm));

        var solicitudes = vm.Asignaturas.Select(a => new SolicitudOfertaInput(
            a.AsignaturaId, a.Incluir, a.CantidadGrupos, a.Cupo)).ToList();
        var proyeccion = generador.Generar(vm.NombreProyeccion, solicitudes);
        TempData[proyeccion.Estado.StartsWith("Completa", StringComparison.Ordinal) ? "Exito" : "Error"] =
            $"Proyección '{proyeccion.Nombre}': {proyeccion.TotalGruposProgramados} de {proyeccion.TotalGruposSolicitados} grupos programados. {proyeccion.Mensaje}";
        return RedirectToAction(nameof(Index), new { id = proyeccion.Id });
    }

    [HttpGet]
    public IActionResult ExportarExcel(int id)
    {
        var proyeccion = db.ProyeccionesOferta.AsNoTracking()
            .Include(p => p.Grupos).ThenInclude(g => g.Asignatura)
            .Include(p => p.Grupos).ThenInclude(g => g.Profesor)
            .Include(p => p.Grupos).ThenInclude(g => g.Salon)
            .Include(p => p.Grupos).ThenInclude(g => g.Sesiones)
            .FirstOrDefault(p => p.Id == id);
        if (proyeccion is null) return NotFound();

        var bytes = excel.Generar(proyeccion);
        var nombre = $"Oferta_Academica_{proyeccion.Id}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre);
    }

    private GenerarOfertaViewModel PrepararGeneracion(GenerarOfertaViewModel vm)
    {
        var anteriores = vm.Asignaturas.ToDictionary(a => a.AsignaturaId, a => a);
        vm.Asignaturas = db.Asignaturas.AsNoTracking().Where(a => a.Activa).OrderBy(a => a.Codigo).ToList().GroupBy(a => a.Codigo, StringComparer.OrdinalIgnoreCase).Select(g => g.OrderBy(a => a.Id).First()).ToList()
            .Select(a => anteriores.TryGetValue(a.Id, out var anterior)
                ? new AsignaturaOfertaOpcionVM
                {
                    AsignaturaId = a.Id, Codigo = a.Codigo, Nombre = a.Nombre, Programa = a.ProgramasCodigosCsv.Length > 0 ? a.ProgramasCodigosCsv : a.ProgramaCodigo,
                    Semestre = a.Semestre, IntensidadHoraria = a.IntensidadHoraria, TipoAsignatura = a.TipoAsignatura, Incluir = anterior.Incluir,
                    CantidadGrupos = anterior.CantidadGrupos, Cupo = anterior.Cupo
                }
                : new AsignaturaOfertaOpcionVM
                {
                    AsignaturaId = a.Id, Codigo = a.Codigo, Nombre = a.Nombre, Programa = a.ProgramasCodigosCsv.Length > 0 ? a.ProgramasCodigosCsv : a.ProgramaCodigo,
                    Semestre = a.Semestre, IntensidadHoraria = a.IntensidadHoraria, TipoAsignatura = a.TipoAsignatura,
                    CantidadGrupos = a.CantidadGrupos,
                    Cupo = a.TipoAsignatura switch
                    {
                        "Teórico-práctica" => 25,
                        "Laboratorio" => 20,
                        "Física laboratorio" => 15,
                        "Física teórica" => 40,
                        _ => 30
                    }
                }).ToList();
        AplicarPlanificacionPregrado(vm);
        return vm;
    }

    private void AplicarPlanificacionPregrado(GenerarOfertaViewModel vm)
    {
        var planes = db.PlanificacionPregrado.AsNoTracking().ToList();
        foreach (var opcion in vm.Asignaturas)
        {
            var plan = planes.FirstOrDefault(p => Normalizar(p.NombreAsignatura) == Normalizar(opcion.Nombre));
            if (plan is null) continue;
            try
            {
                using var json = System.Text.Json.JsonDocument.Parse(plan.DatosJson);
                var root = json.RootElement;
                if (root.TryGetProperty("num_grupos", out var grupos) && grupos.TryGetInt32(out var n) && n > 0)
                    opcion.CantidadGrupos = Math.Clamp(n, 1, 30);
                if (root.TryGetProperty("num_estimados", out var estimados) && estimados.TryGetDouble(out var total) && total > 0)
                    opcion.Cupo = Math.Max(1, (int)Math.Ceiling(total / Math.Max(1, opcion.CantidadGrupos)));
            }
            catch (System.Text.Json.JsonException) { }
        }
    }

    private static string Normalizar(string value)
    {
        var decomposed = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = decomposed.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Trim().ToUpperInvariant();
    }
}
