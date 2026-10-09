namespace OfertaAcademica.Web.ViewModels.Shared;

public class PaginacionViewModel
{
    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 10;
    public int TotalResultados { get; init; }
    public string? Busqueda { get; init; }

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalResultados / (double)TamanoPagina));
    public int Desde => TotalResultados == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;
    public int Hasta => Math.Min(Pagina * TamanoPagina, TotalResultados);

    /// <summary>Ventana de hasta 5 páginas alrededor de la actual.</summary>
    public IEnumerable<int> PaginasVisibles
    {
        get
        {
            var inicio = Math.Max(1, Math.Min(Pagina - 2, TotalPaginas - 4));
            var fin = Math.Min(TotalPaginas, inicio + 4);
            return Enumerable.Range(inicio, fin - inicio + 1);
        }
    }
}
