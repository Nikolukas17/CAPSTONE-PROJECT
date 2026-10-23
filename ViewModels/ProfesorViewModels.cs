namespace OfertaAcademica.Web.ViewModels.Profesores;

public class ProfesorFilaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string? FotoUrl { get; set; }
}

public class ProfesoresIndexViewModel
{
    public List<ProfesorFilaViewModel> Items { get; set; } = new();
    public string? Busqueda { get; set; }
    public int PaginaActual { get; set; } = 1;
    public int PorPagina { get; set; } = 6;
    public int Total { get; set; }
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)PorPagina));
    public int Desde => Total == 0 ? 0 : (PaginaActual - 1) * PorPagina + 1;
    public int Hasta => Math.Min(PaginaActual * PorPagina, Total);
}

public class MateriaProfesorViewModel
{
    public string Codigo { get; set; } = "";
    public string? Nivel { get; set; }
    public string Materia { get; set; } = "";
}

public class ProfesorDetalleViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; } = true;
    public List<MateriaProfesorViewModel> Materias { get; set; } = new();

    public static readonly string[] Dias = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
    public static readonly string[] Franjas = { "7–9", "9–11", "11–1", "1–3", "3–5", "5–7" };

    /// <summary>Disponibilidad[franja][dia]: true = disponible.</summary>
    public bool[][] Disponibilidad { get; set; } =
        Enumerable.Range(0, Franjas.Length).Select(_ => new bool[Dias.Length]).ToArray();
}
