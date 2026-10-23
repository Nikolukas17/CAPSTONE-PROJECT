using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Profesores;

namespace OfertaAcademica.Web.Controllers;

public class ProfesoresController(IAsignaturaService asignaturas) : Controller
{
    private const int PorPagina = 6;
    private static readonly string[] TiposProfesor = ["Docencia", "Carrera"];

    // TODO: reemplazar por un IProfesorService (Services/Interfaces) cuando exista.
    private static readonly List<ProfesorFilaViewModel> Demo = new()
    {
        new() { Id = 1, Nombre = "David Eugenio Andrade",           Correo = "ana.torres@urosario.edu.co",      Tipo = "Docencia" },
        new() { Id = 2, Nombre = "Nicolás Guillermo Avilán Vargas", Correo = "carlos.martinez@urosario.edu.co", Tipo = "Docencia" },
        new() { Id = 3, Nombre = "Norma Constanza Sarmiento",       Correo = "eliana.gomez@urosario.edu.co",    Tipo = "Carrera" },
        new() { Id = 4, Nombre = "Daniel Alfonso Bojacá",           Correo = "jorge.morales@urosario.edu.co",   Tipo = "Carrera" },
        new() { Id = 5, Nombre = "José Alejandro Guerrero",         Correo = "laura.castillo@urosario.edu.co",  Tipo = "Docencia" },
        new() { Id = 6, Nombre = "David Nicolás Castiblanco",       Correo = "miguel.herrera@urosario.edu.co",  Tipo = "Carrera" },
    };

    // Datos completos (teléfono, materias, disponibilidad) de los profesores creados o editados.
    // Solo en memoria, igual que Demo.
    private static readonly Dictionary<int, ProfesorFormViewModel> Fichas = new();

    private static readonly (string Codigo, string? Nivel, string Materia)[] MateriasBase =
    [
        ("11310003", null,       "Cálculo I"),
        ("11310004", null,       "Cálculo II"),
        ("11320003", "11310038", "Cálculo III"),
        ("11310010", null,       "Análisis Real"),
        ("11310020", null,       "Topología"),
        ("11310018", "11320009", "Álgebra Abstracta"),
    ];

    private const string TelefonoBase = "+57 300 123 4567";
    private const string DescripcionBase = "Doctorado en Matemáticas con énfasis en Análisis y Álgebra, Magister en Ciencias Matemáticas y Licenciatura en Matemáticas.";

    public IActionResult Index(string? q, int pagina = 1)
    {
        var query = Demo.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase)
                                  || p.Correo.Contains(q, StringComparison.OrdinalIgnoreCase));

        var total = query.Count();
        var vm = new ProfesoresIndexViewModel
        {
            Busqueda = q,
            PorPagina = PorPagina,
            Total = total,
            PaginaActual = Math.Max(1, pagina),
            Items = query.Skip((Math.Max(1, pagina) - 1) * PorPagina).Take(PorPagina).ToList()
        };
        return View(vm);
    }

    public IActionResult Detalle(int id, bool inactivar = false)
    {
        var fila = Demo.FirstOrDefault(p => p.Id == id);
        if (fila is null) return NotFound();

        var tieneFicha = Fichas.TryGetValue(id, out var ficha);

        var vm = new ProfesorDetalleViewModel
        {
            Id = fila.Id,
            Nombre = fila.Nombre,
            FotoUrl = fila.FotoUrl,
            Telefono = tieneFicha ? ficha!.Telefono ?? "" : TelefonoBase,
            Tipo = "Profesor de " + fila.Tipo,
            Descripcion = tieneFicha ? ficha!.Descripcion ?? "" : DescripcionBase,
            Activo = true,
            Materias = new()
        };

        if (tieneFicha)
        {
            foreach (var materiaId in ficha!.MateriasSeleccionadas)
                if (asignaturas.Obtener(materiaId) is { } a)
                    vm.Materias.Add(new() { Codigo = a.Codigo, Nivel = null, Materia = a.Nombre });
        }
        else
        {
            foreach (var m in MateriasBase)
                vm.Materias.Add(new() { Codigo = m.Codigo, Nivel = m.Nivel, Materia = m.Materia });
        }

        // Sin ficha: lunes–viernes disponible de 7 a 1
        for (var f = 0; f < ProfesorDetalleViewModel.Franjas.Length; f++)
            for (var d = 0; d < ProfesorDetalleViewModel.Dias.Length; d++)
                vm.Disponibilidad[f][d] = tieneFicha ? EstaDisponible(ficha!, f, d) : (f < 3 && d < 5);

        ViewBag.AbrirInactivar = inactivar;
        return View(vm);
    }

    [HttpGet]
    public IActionResult Crear() => View("Formulario", Preparar(new ProfesorFormViewModel()));

    [HttpGet]
    public IActionResult Editar(int id)
    {
        var fila = Demo.FirstOrDefault(p => p.Id == id);
        if (fila is null) return NotFound();

        if (!Fichas.TryGetValue(id, out var vm))
        {
            var disp = DisponibilidadSemanal.Base();
            for (var f = 0; f < ProfesorDetalleViewModel.Franjas.Length; f++)
                for (var d = 0; d < ProfesorDetalleViewModel.Dias.Length; d++)
                {
                    var idx = DisponibilidadSemanal.Indice(f, d);
                    if (idx < disp.Count)
                        disp[idx] = (f < 3 && d < 5) ? EstadoFranja.Disponible : EstadoFranja.NoDisponible;
                }

            var codigos = MateriasBase.Select(m => m.Codigo).ToHashSet();
            var (catalogo, _) = asignaturas.Buscar(null, 1, 1000);

            vm = new ProfesorFormViewModel
            {
                Id = fila.Id,
                Nombre = fila.Nombre,
                Correo = fila.Correo,
                Tipo = fila.Tipo,
                FotoUrl = fila.FotoUrl,
                Telefono = TelefonoBase,
                Descripcion = DescripcionBase,
                MateriasSeleccionadas = catalogo.Where(a => codigos.Contains(a.Codigo)).Select(a => a.Id).ToList(),
                Disponibilidad = disp
            };
        }
        return View("Formulario", Preparar(vm));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Guardar(ProfesorFormViewModel vm)
    {
        if (Demo.Any(p => p.Id != vm.Id && p.Correo.Equals(vm.Correo?.Trim(), StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(vm.Correo), "Ya existe un profesor con este correo.");

        if (vm.Disponibilidad.Count != DisponibilidadSemanal.Total)
            vm.Disponibilidad = DisponibilidadSemanal.Base();

        if (!ModelState.IsValid)
            return View("Formulario", Preparar(vm));

        // TODO: llamar al servicio real. Con la lista demo reemplazamos la fila.
        var nueva = new ProfesorFilaViewModel
        {
            Nombre = vm.Nombre.Trim(),
            Correo = vm.Correo.Trim(),
            Tipo = vm.Tipo,
            FotoUrl = vm.FotoUrl
        };

        if (vm.Id == 0)
        {
            vm.Id = Demo.Count == 0 ? 1 : Demo.Max(p => p.Id) + 1;
            nueva.Id = vm.Id;
            Demo.Add(nueva);
        }
        else
        {
            var pos = Demo.FindIndex(p => p.Id == vm.Id);
            if (pos < 0) return NotFound();
            nueva.Id = vm.Id;
            Demo[pos] = nueva;
        }

        Fichas[vm.Id] = vm;
        TempData["Mensaje"] = nueva.Id == vm.Id && Demo.Count > 0 && vm.Id != 0
            ? $"Guardamos a {nueva.Nombre}."
            : "Guardamos los cambios del profesor.";
        return RedirectToAction(nameof(Detalle), new { id = vm.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Inactivar(int id)
    {
        // TODO: llamar al servicio para marcar al profesor como inactivo.
        TempData["Mensaje"] = "Profesor inactivado.";
        return RedirectToAction(nameof(Detalle), new { id });
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

    private static bool EstaDisponible(ProfesorFormViewModel ficha, int franja, int dia)
    {
        var idx = DisponibilidadSemanal.Indice(franja, dia);
        return idx < ficha.Disponibilidad.Count && ficha.Disponibilidad[idx] == EstadoFranja.Disponible;
    }
}