using Microsoft.AspNetCore.Mvc;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Oferta;

namespace OfertaAcademica.Web.Controllers;

public class OfertaAcademicaController : Controller
{
    public IActionResult Index()
    {
        return View(new OfertaSemanaViewModel
        {
            Titulo = "Semana 7: del 9 al 13 de marzo, 2026",
            Clases = DatosDemo.ClasesSemana()
        });
    }
}
