namespace OfertaAcademica.Web.Models.Entities;

public class ProyeccionOferta
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public string Estado { get; set; } = "Completa";
    public string Mensaje { get; set; } = "";
    public bool EsActual { get; set; }
    public int TotalGruposSolicitados { get; set; }
    public int TotalGruposProgramados { get; set; }
    public List<GrupoOferta> Grupos { get; set; } = [];
    public List<SolicitudOferta> Solicitudes { get; set; } = [];
}

public class GrupoOferta
{
    public int Id { get; set; }
    public int ProyeccionOfertaId { get; set; }
    public int AsignaturaId { get; set; }
    public int ProfesorId { get; set; }
    public string SalonCodigo { get; set; } = "";
    public int NumeroGrupo { get; set; }
    public int Cupo { get; set; }
    public ProyeccionOferta? Proyeccion { get; set; }
    public Asignatura? Asignatura { get; set; }
    public Profesor? Profesor { get; set; }
    public Salon? Salon { get; set; }
    public List<SesionOferta> Sesiones { get; set; } = [];
}

public class SesionOferta
{
    public int Id { get; set; }
    public int GrupoOfertaId { get; set; }
    public DayOfWeek Dia { get; set; }
    /// <summary>Minutos desde medianoche.</summary>
    public int InicioMinutos { get; set; }
    public int FinMinutos { get; set; }
    public GrupoOferta? Grupo { get; set; }
}

public class SolicitudOferta
{
    public int Id { get; set; }
    public int ProyeccionOfertaId { get; set; }
    public int AsignaturaId { get; set; }
    public bool Incluir { get; set; }
    public int CantidadGrupos { get; set; }
    public int Cupo { get; set; }
    public ProyeccionOferta? Proyeccion { get; set; }
    public Asignatura? Asignatura { get; set; }
}
