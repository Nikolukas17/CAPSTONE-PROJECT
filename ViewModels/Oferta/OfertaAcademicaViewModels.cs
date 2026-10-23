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
    /// <summary>rojo | verde | azul | amarillo | rosa | gris</summary>
    public string Color { get; set; } = "gris";

    public string Horario => $"{Inicio:hh\\:mm} - {Fin:hh\\:mm}";
}

public class OfertaAcademicaViewModel
{
    public int Semana { get; set; } = 1;
    public int TotalSemanas { get; set; } = 16;
    public DateTime InicioSemana { get; set; }

    /// <summary>Horas de inicio de cada franja (bloques de DuracionFranja horas).</summary>
    public List<int> Franjas { get; set; } = new() { 7, 9, 11, 13, 15, 17 };
    public int DuracionFranja { get; set; } = 2;

    public List<SesionClaseVM> Sesiones { get; set; } = new();

    /// <summary>Todas las asignaturas que se dictan ese día dentro de la franja.</summary>
    public List<SesionClaseVM> EnFranja(DayOfWeek dia, int horaInicio) =>
        Sesiones
            .Where(s => s.Dia == dia && s.Inicio.Hours >= horaInicio && s.Inicio.Hours < horaInicio + DuracionFranja)
            .OrderBy(s => s.Inicio).ThenBy(s => s.Nombre)
            .ToList();
}
