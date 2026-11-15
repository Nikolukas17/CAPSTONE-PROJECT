using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfertaAcademica.Web.Data;
using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Profesores;

namespace OfertaAcademica.Web.Controllers;

[Authorize]
public class ProfesoresController(IProfesorService profesores, IAsignaturaService asignaturas, GeneradorOfertaService generador,
    AppDbContext db, ICorreoInstitucionalService correo, ILogger<ProfesoresController> logger) : Controller
{
    private const int PorPagina = 6;
    private const int DiasVigenciaEnlace = 14;
    private static readonly string[] TiposProfesor = ["Carrera", "Docencia", "Doctorado", "Cátedra"];

    public IActionResult Index(string? q, int pagina = 1)
    {
        var (items, total) = profesores.Buscar(q, Math.Max(1, pagina), PorPagina);
        return View(new ProfesoresIndexViewModel
        {
            Busqueda = q,
            PorPagina = PorPagina,
            Total = total,
            PaginaActual = Math.Max(1, pagina),
            Items = items.Select(p => new ProfesorFilaViewModel
            {
                Id = p.Id, Nombre = p.Nombre, Correo = p.Correo, Tipo = p.Tipo
            }).ToList()
        });
    }

    public IActionResult Detalle(int id, bool inactivar = false)
    {
        var p = profesores.Obtener(id);
        if (p is null) return NotFound();

        var vm = new ProfesorDetalleViewModel
        {
            Id       = p.Id,
            Nombre   = p.Nombre,
            Telefono = p.Telefono,
            Tipo     = "Profesor de " + p.Tipo,
            Descripcion = p.Perfil,
            Activo   = p.Activo,
            Correo   = p.Correo,
            EsCatedra = DisponibilidadSemanal.EsCatedra(p.Tipo),
            TieneDisponibilidad = DisponibilidadSemanal.TieneFranjasDisponibles(p.Disponibilidad),
            DisponibilidadActualizadaUtc = p.DisponibilidadActualizadaUtc,
            EnlaceDisponibilidadVenceUtc = p.TokenDisponibilidadExpiraUtc > DateTime.UtcNow ? p.TokenDisponibilidadExpiraUtc : null,
            Materias = p.Materias.GroupBy(m => m.Codigo, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First()).Select(m => new MateriaProfesorViewModel
                {
                    Codigo = m.Codigo,
                    Nivel  = m.Nivel == "N/A" ? null : m.Nivel,
                    Materia = m.Nombre
                }).ToList()
        };

        for (var f = 0; f < ProfesorDetalleViewModel.Franjas.Length; f++)
            for (var d = 0; d < ProfesorDetalleViewModel.Dias.Length; d++)
            {
                var idx = DisponibilidadSemanal.Indice(f, d);
                vm.Disponibilidad[f][d] = idx < p.Disponibilidad.Count
                    && p.Disponibilidad[idx] == EstadoFranja.Disponible;
            }

        ViewBag.AbrirInactivar = inactivar;
        return View(vm);
    }

    [HttpGet]
    public IActionResult Crear() => View("Formulario", Preparar(new ProfesorFormViewModel()));

    [HttpGet]
    public IActionResult Editar(int id)
    {
        var p = profesores.Obtener(id);
        if (p is null) return NotFound();

        var (catalogo, _) = asignaturas.Buscar(null, 1, 1000);
        var materiasIds = catalogo
            .Where(a => p.Materias.Any(m => m.Codigo == a.Codigo))
            .Select(a => a.Id)
            .ToList();

        return View("Formulario", Preparar(new ProfesorFormViewModel
        {
            Id          = p.Id,
            Nombre      = p.Nombre,
            Correo      = p.Correo,
            Tipo        = p.Tipo,
            CursosMinimos = p.CursosMinimos,
            CursosMaximos = p.CursosMaximos,
            HorasMinimas = p.HorasMinimas,
            HorasMaximas = p.HorasMaximas,
            Telefono    = p.Telefono,
            Descripcion = p.Perfil,
            MateriasSeleccionadas = materiasIds,
            Disponibilidad = p.Disponibilidad
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Guardar(ProfesorFormViewModel vm)
    {
        if (profesores.ExisteCorreo(vm.Correo?.Trim() ?? "", vm.Id))
            ModelState.AddModelError(nameof(vm.Correo), "Ya existe un profesor con este correo.");

        if (vm.Disponibilidad.Count != DisponibilidadSemanal.Total)
            vm.Disponibilidad = DisponibilidadSemanal.PorTipoProfesor(vm.Tipo);
        if (!TiposProfesor.Contains(vm.Tipo) && !DisponibilidadSemanal.EsCatedra(vm.Tipo))
            ModelState.AddModelError(nameof(vm.Tipo), "Selecciona un tipo de profesor válido.");

        if (!ModelState.IsValid)
            return View("Formulario", Preparar(vm));

        var (catalogo, _) = asignaturas.Buscar(null, 1, 1000);
        var materias = catalogo
            .Where(a => vm.MateriasSeleccionadas.Contains(a.Id))
            .Select(a => new MateriaProfesor(a.Codigo, "N/A", a.Nombre))
            .ToList();

        if (vm.CursosMinimos > vm.CursosMaximos)
            ModelState.AddModelError(nameof(vm.CursosMinimos), "El mínimo de cursos no puede superar el máximo.");
        if (vm.HorasMinimas > vm.HorasMaximas)
            ModelState.AddModelError(nameof(vm.HorasMinimas), "Las horas mínimas no pueden superar las máximas.");
        if (!ModelState.IsValid)
            return View("Formulario", Preparar(vm));

        var entidad = new Profesor
        {
            Id           = vm.Id,
            Nombre       = vm.Nombre.Trim(),
            Correo       = vm.Correo?.Trim() ?? "",
            Tipo         = vm.Tipo,
            CursosMinimos = vm.CursosMinimos,
            CursosMaximos = vm.CursosMaximos,
            HorasMinimas = vm.HorasMinimas,
            HorasMaximas = vm.HorasMaximas,
            Telefono     = vm.Telefono?.Trim() ?? "",
            Perfil       = vm.Descripcion?.Trim() ?? "",
            Activo       = true,
            Materias     = materias,
            Disponibilidad = vm.Disponibilidad
        };

        var guardado = profesores.Guardar(entidad);
        generador.RecalcularProyeccionActual();
        TempData["Mensaje"] = $"Guardamos a {guardado.Nombre}.";
        return RedirectToAction(nameof(Detalle), new { id = guardado.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Inactivar(int id)
    {
        profesores.Inactivar(id);
        generador.RecalcularProyeccionActual();
        TempData["Mensaje"] = "Profesor inactivado.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Genera un enlace personal (vigente 14 días) para que el profesor marque su disponibilidad sin tener cuenta.
    /// Se intenta enviar por correo y, en cualquier caso, se muestra a la coordinación para que pueda compartirlo.
    /// Generar un enlace nuevo invalida el anterior.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnlaceDisponibilidad(int id)
    {
        var p = db.Profesores.FirstOrDefault(x => x.Id == id);
        if (p is null) return NotFound();
        if (!p.Activo)
        {
            TempData["Error"] = "El profesor está inactivo.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        p.TokenDisponibilidadHash = HashToken(token);
        p.TokenDisponibilidadExpiraUtc = DateTime.UtcNow.AddDays(DiasVigenciaEnlace);
        db.SaveChanges();

        var enlace = Url.Action("Registrar", "DisponibilidadProfesor", new { token }, Request.Scheme)!;
        TempData["EnlaceDisponibilidad"] = enlace;

        if (string.IsNullOrWhiteSpace(p.Correo) || p.Correo.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "El profesor no tiene un correo válido: copia el enlace y compártelo por otro medio.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        try
        {
            var resultado = await correo.EnviarEnlaceDisponibilidadAsync(p.Correo, p.Nombre, enlace, p.TokenDisponibilidadExpiraUtc.Value);
            TempData["Exito"] = resultado == ResultadoEnvioCorreo.Enviado
                ? $"Enviamos el enlace a {p.Correo}. También puedes copiarlo y compartirlo por otro medio."
                : "Modo desarrollo: el correo se guardó en App_Data/correos-desarrollo. Copia el enlace para probarlo.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el enlace de disponibilidad al profesor {Id}.", id);
            TempData["Error"] = "No se pudo enviar el correo (revisa la configuración SMTP). Copia el enlace y compártelo por otro medio.";
        }
        return RedirectToAction(nameof(Detalle), new { id });
    }

    internal static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    // ---------- helpers ----------

    private ProfesorFormViewModel Preparar(ProfesorFormViewModel vm)
    {
        vm.Tipos = TiposProfesor.Select(t => new SelectListItem(t, t)).ToList();
        if (DisponibilidadSemanal.EsCatedra(vm.Tipo) && !TiposProfesor.Contains(vm.Tipo))
            vm.Tipos.Add(new SelectListItem(vm.Tipo, vm.Tipo)); // p. ej. "Cátedra simulado"
        if (vm.EsNuevo && vm.CursosMinimos == 0 && vm.CursosMaximos == 5)
        {
            vm.CursosMinimos = vm.Tipo == "Carrera" ? 2 : vm.Tipo == "Docencia" ? 4 : 0;
            vm.CursosMaximos = vm.Tipo == "Carrera" ? 4 : vm.Tipo == "Docencia" ? 5 : DisponibilidadSemanal.EsCatedra(vm.Tipo) ? 4 : 30;
            vm.HorasMinimas = vm.Tipo == "Doctorado" ? 6 : 0;
            vm.HorasMaximas = vm.Tipo == "Carrera" ? 12 : 200;
        }
        var (items, _) = asignaturas.Buscar(null, 1, 1000);
        vm.MateriasDisponibles = items
            .Select(a => new MateriaOpcionVM { Id = a.Id, Codigo = a.Codigo, Nombre = a.Nombre })
            .ToList();
        if (vm.Disponibilidad.Count != DisponibilidadSemanal.Total)
            vm.Disponibilidad = DisponibilidadSemanal.PorTipoProfesor(vm.Tipo);
        return vm;
    }
}
