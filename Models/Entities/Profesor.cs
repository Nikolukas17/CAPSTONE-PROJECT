namespace OfertaAcademica.Web.Models.Entities;

public class Profesor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Tipo { get; set; } = "";          // Carrera | Docencia | Doctorado | Cátedra
    public int CursosMinimos { get; set; } = 0;
    public int CursosMaximos { get; set; } = 99;
    public int HorasMinimas { get; set; } = 0;
    public int HorasMaximas { get; set; } = 200;
    public string Telefono { get; set; } = "";
    public string Perfil { get; set; } = "";
    public bool Activo { get; set; } = true;
    public List<MateriaProfesor> Materias { get; set; } = [];
    public List<EstadoFranja> Disponibilidad { get; set; } = DisponibilidadSemanal.JornadaFija();

    /// <summary>Última vez que se registró la disponibilidad (por la coordinación o por el propio profesor).</summary>
    public DateTime? DisponibilidadActualizadaUtc { get; set; }
    /// <summary>Hash SHA-256 del enlace personal para que el profesor registre su disponibilidad.</summary>
    public string? TokenDisponibilidadHash { get; set; }
    public DateTime? TokenDisponibilidadExpiraUtc { get; set; }
}

public record MateriaProfesor(string Codigo, string Nivel, string Nombre);
