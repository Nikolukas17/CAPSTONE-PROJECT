namespace OfertaAcademica.Web.ViewModels.Oferta;

public class SesionClaseVM
{
    public int Id { get; set; }
    public int AsignaturaId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Profesor { get; set; } = "";
    public string Salon { get; set; } = "";
    public string Programa { get; set; } = "";
    public string Grupo { get; set; } = "";
    public int Cupo { get; set; }
    public DayOfWeek Dia { get; set; }
    public TimeSpan Inicio { get; set; }
    public TimeSpan Fin { get; set; }
    public string Color { get; set; } = "gris";
    public string Horario => $"{Inicio:hh\\:mm} - {Fin:hh\\:mm}";
}

public class OfertaAcademicaViewModel
{
    public int Semana { get; set; } = 1;
    public int TotalSemanas { get; set; } = 16;
    public DateTime InicioSemana { get; set; }
    public int? ProyeccionId { get; set; }
    public string NombreProyeccion { get; set; } = "";
    public string EstadoProyeccion { get; set; } = "Sin proyección";
    public string MensajeProyeccion { get; set; } = "";
    public int TotalGruposProgramados { get; set; }
    public int TotalGruposSolicitados { get; set; }
    public List<ProyeccionResumenVM> Historial { get; set; } = [];
    public List<int> Franjas { get; set; } = Enumerable.Range(0, 24).Select(i => 7 * 60 + i * 30).ToList();
    public int DuracionFranja { get; set; } = 30;
    public List<SesionClaseVM> Sesiones { get; set; } = [];

    public List<SesionClaseVM> EnFranja(DayOfWeek dia, int inicioMinutos) =>
        Sesiones.Where(s => s.Dia == dia && s.Inicio.TotalMinutes < inicioMinutos + DuracionFranja && s.Fin.TotalMinutes > inicioMinutos)
            .OrderBy(s => s.Inicio).ThenBy(s => s.Nombre).ToList();
}

public class ProyeccionResumenVM
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public string Estado { get; set; } = "";
}
