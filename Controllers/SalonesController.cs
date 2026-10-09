using Microsoft.AspNetCore.Mvc;

namespace OfertaAcademica.Web.Controllers;

public class SalonesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}