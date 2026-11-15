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

    /// <summary>Franjas de 30 min que forman un bloque de 2 horas (vista de disponibilidad de profesores).</summary>
    public const int FranjasPorBloque = 4;
    public static readonly string[] Bloques = Enumerable.Range(0, 24 / FranjasPorBloque)
        .Select(b => $"{7 + 2 * b}:00–{9 + 2 * b}:00").ToArray();

    public static int Total => Dias.Length * Franjas.Length;

    public static int Indice(int franja, int dia) => franja * Dias.Length + dia;

    public static List<EstadoFranja> Vacia() =>
        Enumerable.Repeat(EstadoFranja.NoDisponible, Total).ToList();

    /// <summary>Todos los días (lunes a sábado) de 7:00 a 19:00 disponibles. Se usa para salones.</summary>
    public static List<EstadoFranja> Base() => Rango(0, 5, 7 * 60, 19 * 60);

    /// <summary>Asignaturas en general: lunes a viernes de 7:00 a 19:00 (el sábado se reserva para Capstone).</summary>
    public static List<EstadoFranja> AsignaturaEstandar() => Rango(0, 4, 7 * 60, 19 * 60);

    /// <summary>Capstone Project: únicamente sábado de 7:00 a 19:00.</summary>
    public static List<EstadoFranja> SoloSabado() => Rango(5, 5, 7 * 60, 19 * 60);

    /// <summary>
    /// Jornada fija de profesores de carrera, docencia y doctorado: 7:00 a 17:00.
    /// Incluye el sábado porque solo Capstone puede programarse ese día.
    /// </summary>
    public static List<EstadoFranja> JornadaFija() => Rango(0, 5, 7 * 60, 17 * 60);

    /// <summary>Disponibilidad inicial según el tipo de profesor. Cátedra arranca vacía: se registra con la coordinación.</summary>
    public static List<EstadoFranja> PorTipoProfesor(string? tipo) =>
        EsCatedra(tipo) ? Vacia() : JornadaFija();

    public static bool EsCatedra(string? tipo) =>
        !string.IsNullOrWhiteSpace(tipo) && tipo.StartsWith("Cátedra", StringComparison.OrdinalIgnoreCase);

    public static bool EsCapstone(string? nombreAsignatura) =>
        !string.IsNullOrWhiteSpace(nombreAsignatura) && nombreAsignatura.Contains("CAPSTONE", StringComparison.OrdinalIgnoreCase);

    public static List<EstadoFranja> PorAsignatura(string? nombreAsignatura) =>
        EsCapstone(nombreAsignatura) ? SoloSabado() : AsignaturaEstandar();

    public static bool TieneFranjasDisponibles(IEnumerable<EstadoFranja>? estados) =>
        estados?.Any(e => e == EstadoFranja.Disponible) == true;

    /// <summary>Marca como disponibles los días [diaDesde, diaHasta] (0 = lunes) entre dos horas expresadas en minutos.</summary>
    public static List<EstadoFranja> Rango(int diaDesde, int diaHasta, int inicioMinutos, int finMinutos)
    {
        var lista = Vacia();
        for (var franja = 0; franja < Franjas.Length; franja++)
        {
            var inicioFranja = 7 * 60 + franja * 30;
            if (inicioFranja < inicioMinutos || inicioFranja + 30 > finMinutos) continue;
            for (var dia = diaDesde; dia <= diaHasta; dia++)
                lista[Indice(franja, dia)] = EstadoFranja.Disponible;
        }
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
    public List<EstadoFranja> Disponibilidad { get; set; } = DisponibilidadSemanal.AsignaturaEstandar();
    /// <summary>
    /// "Aula general" = no necesita salón especializado (el salón lo asigna la universidad);
    /// "SALAS" / "LABORATORIO" = tipo de salón especializado; "Cualquiera" = sala o laboratorio.
    /// </summary>
    public string TipoSalonRequerido { get; set; } = TiposSalonRequerido.AulaGeneral;
    public string TipoAsignatura { get; set; } = "Teórica";
    public string SalonesPermitidosCsv { get; set; } = "";
}

public static class TiposSalonRequerido
{
    public const string AulaGeneral = "Aula general";
    public const string Cualquiera = "Cualquiera";
    public const string Salas = "SALAS";
    public const string Laboratorio = "LABORATORIO";

    public static readonly string[] Todos = [AulaGeneral, Salas, Laboratorio, Cualquiera];

    public static string Etiqueta(string tipo) => tipo switch
    {
        AulaGeneral => "Aula general (la asigna la universidad)",
        Salas => "Sala de cómputo",
        Laboratorio => "Laboratorio",
        Cualquiera => "Sala de cómputo o laboratorio",
        _ => tipo
    };
}
