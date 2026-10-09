using Microsoft.AspNetCore.Mvc;

namespace OfertaAcademica.Web.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index() => View();
}
