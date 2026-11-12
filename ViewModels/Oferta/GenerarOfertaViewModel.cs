using System.ComponentModel.DataAnnotations;

namespace OfertaAcademica.Web.ViewModels.Oferta;

public class GenerarOfertaViewModel
{
    [Required, StringLength(100)]
    public string NombreProyeccion { get; set; } = $"Proyección {DateTime.Now:yyyy-MM-dd HH:mm}";

    public List<AsignaturaOfertaOpcionVM> Asignaturas { get; set; } = [];
}

public class AsignaturaOfertaOpcionVM
{
    public int AsignaturaId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Programa { get; set; } = "";
    public string? Semestre { get; set; }
    public int IntensidadHoraria { get; set; }
    public string TipoAsignatura { get; set; } = "Teórica";
    public bool Incluir { get; set; } = true;
    [Range(1, 30)] public int CantidadGrupos { get; set; } = 1;
    [Range(1, 500)] public int Cupo { get; set; } = 30;
}

public record SolicitudOfertaInput(int AsignaturaId, bool Incluir, int CantidadGrupos, int Cupo);
