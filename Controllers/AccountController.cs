using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfertaAcademica.Web.Data;
using OfertaAcademica.Web.Models.Entities;
using OfertaAcademica.Web.Services;
using OfertaAcademica.Web.ViewModels.Account;

namespace OfertaAcademica.Web.Controllers;

public class AccountController(AppDbContext db, ICorreoInstitucionalService correo, IConfiguration configuration) : Controller
{
    private const string DominioInstitucional = "@urosario.edu.co";
    private readonly PasswordHasher<UsuarioAplicacion> _hasher = new();

    [HttpGet] public IActionResult Login(string? returnUrl = null) { ViewData["ReturnUrl"] = returnUrl; return View(new LoginViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);
        var email = vm.Correo.Trim().ToLowerInvariant();
        var user = await db.UsuariosAplicacion.SingleOrDefaultAsync(u => u.Correo == email && u.Activo);
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Contrasena) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(vm);
        }
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.NombreCompleto),
            new(ClaimTypes.Email, user.Correo), new("cargo", user.Cargo)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, AllowRefresh = true });
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet] public IActionResult Registro() => View(new RegistroViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel vm)
    {
        vm.Correo = vm.Correo.Trim().ToLowerInvariant();
        if (!vm.Correo.EndsWith(DominioInstitucional, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(vm.Correo), "Solo se permiten correos @urosario.edu.co.");
        if (await db.UsuariosAplicacion.AnyAsync(u => u.Correo == vm.Correo))
            ModelState.AddModelError(nameof(vm.Correo), "Ya existe una cuenta con ese correo.");
        if (!ModelState.IsValid) return View(vm);
        var user = new UsuarioAplicacion { NombreCompleto = vm.NombreCompleto.Trim(), Cargo = vm.Cargo.Trim(), Correo = vm.Correo };
        user.PasswordHash = _hasher.HashPassword(user, vm.Contrasena);
        db.UsuariosAplicacion.Add(user);
        await db.SaveChangesAsync();
        TempData["Exito"] = "Tu cuenta quedó creada. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet] public IActionResult Olvide() => View(new OlvideViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Olvide(OlvideViewModel vm)
    {
        vm.Correo = vm.Correo.Trim().ToLowerInvariant();
        if (!ModelState.IsValid) return View(vm);
        var user = await db.UsuariosAplicacion.SingleOrDefaultAsync(u => u.Correo == vm.Correo && u.Activo);
        // Respuesta neutra para no revelar si una cuenta está registrada.
        if (user is not null)
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
            db.TokensRecuperacion.Add(new TokenRecuperacionPassword { UsuarioAplicacionId = user.Id, TokenHash = hash, ExpiraUtc = DateTime.UtcNow.AddMinutes(30) });
            await db.SaveChangesAsync();
            var link = Url.Action(nameof(Restablecer), "Account", new { token = raw, correo = user.Correo }, Request.Scheme)!;
            try { await correo.EnviarRecuperacionAsync(user.Correo, user.NombreCompleto, link); }
            catch (Exception) { ModelState.AddModelError(string.Empty, "No fue posible enviar el correo de recuperación. Verifica la configuración SMTP o contacta al administrador."); return View(vm); }
        }
        ViewData["Enviado"] = true;
        return View(new OlvideViewModel { Correo = vm.Correo });
    }

    [HttpGet] public IActionResult Restablecer(string token, string correo) => View(new RestablecerPasswordViewModel { Token = token, Correo = correo });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Restablecer(RestablecerPasswordViewModel vm)
    {
        vm.Correo = vm.Correo.Trim().ToLowerInvariant();
        if (!ModelState.IsValid) return View(vm);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(vm.Token)));
        var token = await db.TokensRecuperacion.Include(t => t.UsuarioAplicacion)
            .SingleOrDefaultAsync(t => t.TokenHash == hash && !t.Usado && t.ExpiraUtc > DateTime.UtcNow && t.UsuarioAplicacion.Correo == vm.Correo);
        if (token is null) { ModelState.AddModelError(string.Empty, "El enlace no es válido o ha expirado. Solicita uno nuevo."); return View(vm); }
        token.UsuarioAplicacion.PasswordHash = _hasher.HashPassword(token.UsuarioAplicacion, vm.NuevaContrasena);
        token.Usado = true;
        await db.SaveChangesAsync();
        TempData["Exito"] = "Tu contraseña fue actualizada. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(id, out var userId)) return Challenge();
        var user = await db.UsuariosAplicacion.FindAsync(userId);
        if (user is null) return NotFound();
        return View(new PerfilViewModel { Nombre = user.NombreCompleto, Correo = user.Correo, Cargo = user.Cargo });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSesion()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
