using Microsoft.AspNetCore.Mvc;
using OfertaAcademica.Web.ViewModels.Dashboard;

namespace OfertaAcademica.Web.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index()
    {
        var vm = new DashboardViewModel
        {
            // TODO: reemplazar por el usuario autenticado (p. ej. User.FindFirst("nombre")?.Value)
            NombreUsuario = User.Identity?.IsAuthenticated == true ? User.Identity!.Name! : "Jeimy Alexandra",
            Pasos = new()
            {
                new("bi-database",     "tono-rojo",    "El sistema utiliza la información académica", "En la Facultad de Ciencias e Ingeniería para gestionar la oferta académica."),
                new("bi-people",       "tono-naranja", "Se alimenta con datos",                       "Profesores, estudiantes y salones disponibles como fuente de información."),
                new("bi-gear",         "tono-azul",    "Procesa la información",                      "Aplica reglas y restricciones para generar horarios optimizados y sin conflictos."),
                new("bi-book",         "tono-verde",   "Genera la oferta académica",                  "Organiza y publica la programación de cursos por semestre de forma automática."),
                new("bi-bar-chart",    "tono-rojo",    "Facilita la toma de decisiones",              "Ofrece reportes y análisis para optimizar recursos y planeación académica."),
            }
        };
        return View(vm);
    }
}
