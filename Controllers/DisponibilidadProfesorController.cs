using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfertaAcademica.Web.Data;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Controllers;

/// <summary>
/// Página pública (sin cuenta) en la que un profesor registra su disponibilidad
/// usando el enlace personal que le envía la coordinación desde su ficha.
/// </summary>
[AllowAnonymous]
[Route("disponibilidad")]
public class DisponibilidadProfesorController(AppDbContext db, ILogger<DisponibilidadProfesorController> logger) : Controller
{
    public sealed class RegistroDisponibilidadViewModel
    {
        public string Token { get; set; } = "";
        public string NombreProfesor { get; set; } = "";
        public DateTime VenceUtc { get; set; }
        public bool Guardado { get; set; }
        public List<EstadoFranja> Disponibilidad { get; set; } = DisponibilidadSemanal.Vacia();
    }

    [HttpGet("{token}")]
    public IActionResult Registrar(string token)
    {
        var profesor = BuscarPorToken(token);
        if (profesor is null) return View("EnlaceInvalido");
        return View(new RegistroDisponibilidadViewModel
        {
            Token = token,
            NombreProfesor = profesor.Nombre,
            VenceUtc = profesor.TokenDisponibilidadExpiraUtc!.Value,
            Disponibilidad = profesor.Disponibilidad.Count == DisponibilidadSemanal.Total
                ? profesor.Disponibilidad.ToList() : DisponibilidadSemanal.Vacia()
        });
    }

    [HttpPost("{token}"), ValidateAntiForgeryToken]
    public IActionResult Registrar(string token, List<EstadoFranja> disponibilidad)
    {
        var profesor = BuscarPorToken(token);
        if (profesor is null) return View("EnlaceInvalido");

        if (disponibilidad.Count != DisponibilidadSemanal.Total)
            return BadRequest();
        // El profesor solo puede marcar disponible / no disponible.
        profesor.Disponibilidad = disponibilidad
            .Select(e => e == EstadoFranja.Disponible ? EstadoFranja.Disponible : EstadoFranja.NoDisponible).ToList();
        profesor.DisponibilidadActualizadaUtc = DateTime.UtcNow;
        db.SaveChanges();
        logger.LogInformation("El profesor {Id} actualizó su disponibilidad mediante enlace.", profesor.Id);

        return View(new RegistroDisponibilidadViewModel
        {
            Token = token,
            NombreProfesor = profesor.Nombre,
            VenceUtc = profesor.TokenDisponibilidadExpiraUtc!.Value,
            Disponibilidad = profesor.Disponibilidad.ToList(),
            Guardado = true
        });
    }

    private Profesor? BuscarPorToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) return null;
        var hash = ProfesoresController.HashToken(token);
        var ahora = DateTime.UtcNow;
        return db.Profesores.FirstOrDefault(p =>
            p.Activo && p.TokenDisponibilidadHash == hash && p.TokenDisponibilidadExpiraUtc > ahora);
    }
}
