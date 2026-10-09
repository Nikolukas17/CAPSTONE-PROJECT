using Microsoft.AspNetCore.Mvc;
using OfertaAcademica.Web.ViewModels.Account;

namespace OfertaAcademica.Web.Controllers;

/// <summary>
/// SOLO INTERFAZ: aún no hay autenticación real. Los POST validan el formulario
/// y redirigen para poder recorrer el flujo de pantallas.
/// </summary>
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Registro() => View(new RegistroViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Registro(RegistroViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        TempData["Exito"] = "Tu cuenta quedó creada. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Olvide() => View(new OlvideViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Olvide(OlvideViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        ViewData["Enviado"] = true;
        return View(new OlvideViewModel { Correo = vm.Correo });
    }

    [HttpGet]
    public IActionResult Perfil() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CerrarSesion() => RedirectToAction("Index", "Home");
}
