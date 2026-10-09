using Microsoft.AspNetCore.Mvc;

namespace OfertaAcademica.Web.Controllers;

public class OfertaAcademicaController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}