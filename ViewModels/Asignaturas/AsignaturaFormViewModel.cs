using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.ViewModels.Asignaturas;

public class AsignaturaFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Escribe el código de la asignatura.")]
    [StringLength(15, MinimumLength = 5, ErrorMessage = "El código debe tener entre 5 y 15 caracteres.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "El código solo puede tener números.")]
    [Display(Name = "Código de la asignatura")]
    public string Codigo { get; set; } = "";

    [StringLength(15, ErrorMessage = "El código compartido admite hasta 15 caracteres.")]
    [RegularExpression(@"^\d*$", ErrorMessage = "El código solo puede tener números.")]
    [Display(Name = "Código compartido")]
    public string? CodigoCompartido { get; set; }

    [Required(ErrorMessage = "Escribe el nombre de la asignatura.")]
    [StringLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")]
    [Display(Name = "Nombre de la asignatura")]
    public string Nombre { get; set; } = "";

    [Display(Name = "Tipología")]
    public Tipologia Tipologia { get; set; } = Tipologia.Obligatoria;

    [Required(ErrorMessage = "Selecciona un programa.")]
    [Display(Name = "Programa / Pregrado")]
    public string ProgramaCodigo { get; set; } = "";

    [Range(1, 12, ErrorMessage = "Los créditos deben estar entre 1 y 12.")]
    [Display(Name = "Número de créditos")]
    public int Creditos { get; set; } = 3;

    [Range(1, 20, ErrorMessage = "La intensidad debe estar entre 1 y 20 horas.")]
    [Display(Name = "Intensidad horaria semanal (hrs)")]
    public int IntensidadHoraria { get; set; } = 4;

    public List<EstadoFranja> Disponibilidad { get; set; } = DisponibilidadSemanal.Base();

    // ----- Solo para pintar la vista (no se enlazan desde el formulario) -----
    [BindNever] public bool ModoEdicion { get; set; }
    [BindNever] public List<SelectListItem> Programas { get; set; } = [];
    [BindNever] public List<SelectListItem> Tipologias { get; set; } = [];
    [BindNever] public bool EsNueva => Id == 0;

    public static AsignaturaFormViewModel Desde(Asignatura a) => new()
    {
        Id = a.Id,
        Codigo = a.Codigo,
        CodigoCompartido = a.CodigoCompartido,
        Nombre = a.Nombre,
        Tipologia = a.Tipologia,
        ProgramaCodigo = a.ProgramaCodigo,
        Creditos = a.Creditos,
        IntensidadHoraria = a.IntensidadHoraria,
        Disponibilidad = [.. a.Disponibilidad]
    };

    public Asignatura ComoEntidad() => new()
    {
        Id = Id,
        Codigo = Codigo.Trim(),
        CodigoCompartido = (CodigoCompartido ?? "").Trim(),
        Nombre = Nombre.Trim().ToUpperInvariant(),
        Tipologia = Tipologia,
        ProgramaCodigo = ProgramaCodigo,
        Creditos = Creditos,
        IntensidadHoraria = IntensidadHoraria,
        Disponibilidad = Disponibilidad.Count == DisponibilidadSemanal.Total
            ? [.. Disponibilidad]
            : DisponibilidadSemanal.Base()
    };
}
