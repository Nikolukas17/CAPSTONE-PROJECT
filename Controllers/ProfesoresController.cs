using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Profesores;

namespace OfertaAcademica.Web.Controllers;

public class ProfesoresController(IProfesorService profesores, IAsignaturaService asignaturas) : Controller
{
    private const int PorPagina = 6;
    private static readonly string[] TiposProfesor = ["Docencia", "Carrera"];

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
            Materias = p.Materias.Select(m => new MateriaProfesorViewModel
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
            vm.Disponibilidad = DisponibilidadSemanal.Base();

        if (!ModelState.IsValid)
            return View("Formulario", Preparar(vm));

        var (catalogo, _) = asignaturas.Buscar(null, 1, 1000);
        var materias = catalogo
            .Where(a => vm.MateriasSeleccionadas.Contains(a.Id))
            .Select(a => new MateriaProfesor(a.Codigo, "N/A", a.Nombre))
            .ToList();

        var entidad = new Profesor
        {
            Id           = vm.Id,
            Nombre       = vm.Nombre.Trim(),
            Correo       = vm.Correo?.Trim() ?? "",
            Tipo         = vm.Tipo,
            Telefono     = vm.Telefono?.Trim() ?? "",
            Perfil       = vm.Descripcion?.Trim() ?? "",
            Activo       = true,
            Materias     = materias,
            Disponibilidad = vm.Disponibilidad
        };

        var guardado = profesores.Guardar(entidad);
        TempData["Mensaje"] = $"Guardamos a {guardado.Nombre}.";
        return RedirectToAction(nameof(Detalle), new { id = guardado.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Inactivar(int id)
    {
        profesores.Inactivar(id);
        TempData["Mensaje"] = "Profesor inactivado.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------

    private ProfesorFormViewModel Preparar(ProfesorFormViewModel vm)
    {
        vm.Tipos = TiposProfesor.Select(t => new SelectListItem(t, t)).ToList();
        var (items, _) = asignaturas.Buscar(null, 1, 1000);
        vm.MateriasDisponibles = items
            .Select(a => new MateriaOpcionVM { Id = a.Id, Codigo = a.Codigo, Nombre = a.Nombre })
            .ToList();
        if (vm.Disponibilidad.Count != DisponibilidadSemanal.Total)
            vm.Disponibilidad = DisponibilidadSemanal.Base();
        return vm;
    }
}
