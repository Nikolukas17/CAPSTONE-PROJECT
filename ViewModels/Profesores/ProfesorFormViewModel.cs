using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.ViewModels.Profesores;

public class ProfesorFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Escribe el nombre completo del profesor.")]
    [StringLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Escribe el correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = "";

    [StringLength(30, ErrorMessage = "El teléfono admite hasta 30 caracteres.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Selecciona el tipo de profesor.")]
    [Display(Name = "Tipo")]
    public string Tipo { get; set; } = "";

    [StringLength(300, ErrorMessage = "La descripción admite hasta 300 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Url(ErrorMessage = "Escribe una URL válida (https://...).")]
    [Display(Name = "Foto (URL)")]
    public string? FotoUrl { get; set; }

    /// <summary>Ids de las asignaturas que puede dictar.</summary>
    public List<int> MateriasSeleccionadas { get; set; } = [];

    /// <summary>Misma rejilla que usa Asignaturas (franjas x días).</summary>
    public List<EstadoFranja> Disponibilidad { get; set; } = DisponibilidadSemanal.Base();

    // ----- Solo para pintar la vista -----
    [BindNever] public List<SelectListItem> Tipos { get; set; } = [];
    [BindNever] public List<MateriaOpcionVM> MateriasDisponibles { get; set; } = [];
    [BindNever] public bool EsNuevo => Id == 0;
}

public class MateriaOpcionVM
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
}
