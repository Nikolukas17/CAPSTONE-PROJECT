namespace OfertaAcademica.Web.Models.Entities;

public enum Tipologia
{
    Obligatoria,
    Complementaria,
    Electiva
}

public enum EstadoFranja
{
    Disponible,
    Ocupado,
    NoDisponible
}

/// <summary>
/// Cuadrícula semanal de 24 franjas de 30 minutos x 6 días (Lunes–Sábado).
/// Se guarda aplanada: índice = franja * 6 + día.
/// </summary>
public static class DisponibilidadSemanal
{
    public static readonly string[] Dias = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
    public static readonly string[] Franjas = Enumerable.Range(0, 24).Select(i => $"{7 + i / 2:00}:{(i % 2) * 30:00}–{7 + (i + 1) / 2:00}:{((i + 1) % 2) * 30:00}").ToArray();

    public static int Total => Dias.Length * Franjas.Length;

    public static int Indice(int franja, int dia) => franja * Dias.Length + dia;

    public static List<EstadoFranja> Vacia() =>
        Enumerable.Repeat(EstadoFranja.NoDisponible, Total).ToList();

    /// <summary>Por defecto, todos los días de 7:00 a 19:00 disponibles.</summary>
    public static List<EstadoFranja> Base()
    {
        var lista = Vacia();
        for (var franja = 0; franja < 24; franja++)
            for (var dia = 0; dia < 6; dia++)
                lista[Indice(franja, dia)] = EstadoFranja.Disponible;
        return lista;
    }
}

public class Asignatura
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string CodigoCompartido { get; set; } = "";
    public string Nombre { get; set; } = "";
    public Tipologia Tipologia { get; set; }
    public string ProgramaCodigo { get; set; } = "";
    /// <summary>Programas asociados al mismo registro, separados por coma.</summary>
    public string ProgramasCodigosCsv { get; set; } = "";
    public int CantidadGrupos { get; set; } = 1;
    /// <summary>Disponibilidad independiente por grupo, serializada como JSON.</summary>
    public string DisponibilidadesGruposJson { get; set; } = "";
    /// <summary>Semestre curricular ("1".."10"); null para electivas sin semestre fijo.</summary>
    public string? Semestre { get; set; }
    public int Creditos { get; set; }
    public int IntensidadHoraria { get; set; }
    public bool Activa { get; set; } = true;
    public List<EstadoFranja> Disponibilidad { get; set; } = DisponibilidadSemanal.Base();
    public string TipoSalonRequerido { get; set; } = "Cualquiera";
    public string TipoAsignatura { get; set; } = "Teórica";
    public string SalonesPermitidosCsv { get; set; } = "";
}
