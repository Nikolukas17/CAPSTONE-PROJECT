using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.ViewModels.Salones;

public class SalonFormViewModel
{
    /// <summary>El código es la llave: solo se escribe al crear.</summary>
    public bool EsNuevo { get; set; } = true;

    [Required(ErrorMessage = "Escribe el código del salón.")]
    [StringLength(10, MinimumLength = 3, ErrorMessage = "El código debe tener entre 3 y 10 caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9_]+$", ErrorMessage = "El código solo puede tener letras, números y guion bajo.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = "";

    [Required(ErrorMessage = "Escribe el nombre del salón.")]
    [StringLength(80, ErrorMessage = "El nombre admite hasta 80 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Selecciona el tipo de salón.")]
    [Display(Name = "Tipo")]
    public string Tipo { get; set; } = "";

    [Range(0, 500, ErrorMessage = "Los equipos deben estar entre 0 y 500.")]
    [Display(Name = "Equipos")]
    public int Equipos { get; set; }

    [Range(1, 500, ErrorMessage = "La capacidad debe estar entre 1 y 500.")]
    [Display(Name = "Capacidad")]
    public int Capacidad { get; set; } = 20;

    [BindNever] public List<SelectListItem> Tipos { get; set; } = [];

    public static SalonFormViewModel DesdeEntidad(Salon s) => new()
    {
        EsNuevo = false,
        Codigo = s.Codigo,
        Nombre = s.Nombre,
        Tipo = s.Tipo,
        Equipos = s.Equipos,
        Capacidad = s.Capacidad
    };

    public Salon ComoEntidad() => new()
    {
        Codigo = Codigo.Trim().ToUpperInvariant(),
        Nombre = Nombre.Trim().ToUpperInvariant(),
        Tipo = Tipo,
        Equipos = Equipos,
        Capacidad = Capacidad,
        Activo = true
    };
}
