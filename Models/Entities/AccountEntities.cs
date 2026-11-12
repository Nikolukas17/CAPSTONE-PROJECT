namespace OfertaAcademica.Web.Models.Entities;

public class UsuarioAplicacion
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Correo { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime FechaRegistroUtc { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
}

public class TokenRecuperacionPassword
{
    public int Id { get; set; }
    public int UsuarioAplicacionId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiraUtc { get; set; }
    public bool Usado { get; set; }
    public UsuarioAplicacion UsuarioAplicacion { get; set; } = null!;
}

/// <summary>Fila íntegra de la hoja Pregrado del Modelo Planificación Oferta.</summary>
public class PlanificacionPregrado
{
    public int Id { get; set; }
    public string NombreAsignatura { get; set; } = "";
    public string DatosJson { get; set; } = "{}";
}
