using OfertaAcademica.Web.Services;

namespace OfertaAcademica.Web.ViewModels.Oferta;

public class OfertaSemanaViewModel
{
    public string Titulo { get; init; } = "";
    public int[] Horas { get; init; } = [7, 9, 11, 13, 15, 17];
    public int[] NumerosDia { get; init; } = [9, 10, 11, 12, 13];
    public List<DatosDemo.ClaseDemo> Clases { get; init; } = [];
}
