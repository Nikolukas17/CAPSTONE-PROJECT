using Microsoft.AspNetCore.Mvc;

namespace OfertaAcademica.Web.Controllers;

public class ProfesoresController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}