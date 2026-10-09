using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.ViewModels.Shared;

public class DisponibilidadGridViewModel
{
    /// <summary>Nombre del campo a enviar en el formulario (p. ej. "Disponibilidad").</summary>
    public string NombreCampo { get; init; } = "Disponibilidad";
    public List<EstadoFranja> Estados { get; init; } = DisponibilidadSemanal.Vacia();
    /// <summary>Si es true la rejilla genera inputs ocultos y las celdas se pueden alternar.</summary>
    public bool Editable { get; init; }
}
