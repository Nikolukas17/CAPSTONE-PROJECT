namespace OfertaAcademica.Web.Models.Entities;

public class Profesor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Tipo { get; set; } = "";          // Docencia | Carrera
    public string Telefono { get; set; } = "";
    public string Perfil { get; set; } = "";
    public bool Activo { get; set; } = true;
    public List<MateriaProfesor> Materias { get; set; } = [];
    public List<EstadoFranja> Disponibilidad { get; set; } = DisponibilidadSemanal.Base();
}

public record MateriaProfesor(string Codigo, string Nivel, string Nombre);
