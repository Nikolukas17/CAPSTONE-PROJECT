using Microsoft.AspNetCore.Mvc;

namespace OfertaAcademica.Web.Controllers;

public class InicioController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}