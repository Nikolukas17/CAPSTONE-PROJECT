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
/// Cuadrícula semanal de 6 franjas x 6 días (Lunes–Sábado).
/// Se guarda aplanada: índice = franja * 6 + día.
/// </summary>
public static class DisponibilidadSemanal
{
    public static readonly string[] Dias = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
    public static readonly string[] Franjas = ["7–9", "9–11", "11–1", "1–3", "3–5", "5–7"];

    public static int Total => Dias.Length * Franjas.Length;

    public static int Indice(int franja, int dia) => franja * Dias.Length + dia;

    public static List<EstadoFranja> Vacia() =>
        Enumerable.Repeat(EstadoFranja.NoDisponible, Total).ToList();

    /// <summary>Lunes–Viernes de 7 a 1 disponible; el resto no disponible.</summary>
    public static List<EstadoFranja> Base()
    {
        var lista = Vacia();
        for (var franja = 0; franja < 3; franja++)
            for (var dia = 0; dia < 5; dia++)
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
    public int Creditos { get; set; }
    public int IntensidadHoraria { get; set; }
    public bool Activa { get; set; } = true;
    public List<EstadoFranja> Disponibilidad { get; set; } = DisponibilidadSemanal.Base();
}
