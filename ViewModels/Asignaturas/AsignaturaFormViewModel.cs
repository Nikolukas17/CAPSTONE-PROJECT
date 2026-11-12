using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.ViewModels.Asignaturas;

public class AsignaturaFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Escribe el código de la asignatura.")]
    [StringLength(15, MinimumLength = 2, ErrorMessage = "El código debe tener entre 2 y 15 caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "El código solo puede contener letras y números.")]
    [Display(Name = "Código de la asignatura")]
    public string Codigo { get; set; } = "";

    [StringLength(15, ErrorMessage = "El código compartido admite hasta 15 caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9]*$", ErrorMessage = "El código compartido solo puede contener letras y números.")]
    [Display(Name = "Código compartido")]
    public string? CodigoCompartido { get; set; }

    [Required(ErrorMessage = "Escribe el nombre de la asignatura.")]
    [StringLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")]
    [Display(Name = "Nombre de la asignatura")]
    public string Nombre { get; set; } = "";

    [Display(Name = "Tipología")]
    public Tipologia Tipologia { get; set; } = Tipologia.Obligatoria;

    [MinLength(1, ErrorMessage = "Selecciona al menos un programa.")]
    [Display(Name = "Programas / Pregrados")]
    public List<string> ProgramasSeleccionados { get; set; } = [];

    [Display(Name = "Semestre")]
    [StringLength(40, ErrorMessage = "El semestre admite hasta 40 caracteres.")]
    public string? Semestre { get; set; }

    public List<int> PrerrequisitosSeleccionados { get; set; } = [];
    public List<int> CorrequisitosSeleccionados { get; set; } = [];

    [BindNever] public List<SelectListItem> AsignaturasRelacionables { get; set; } = [];

    [Range(1, 12, ErrorMessage = "Los créditos deben estar entre 1 y 12.")]
    [Display(Name = "Número de créditos")]
    public int Creditos { get; set; } = 3;

    [Range(1, 20, ErrorMessage = "La intensidad debe estar entre 1 y 20 horas.")]
    [Display(Name = "Intensidad horaria semanal (hrs)")]
    public int IntensidadHoraria { get; set; } = 4;

    public List<EstadoFranja> Disponibilidad { get; set; } = DisponibilidadSemanal.Base();
    public string TipoSalonRequerido { get; set; } = "Cualquiera";
    public string TipoAsignatura { get; set; } = "Teórica";
    public string? SalonesPermitidosCsv { get; set; }
    [Range(1, 30, ErrorMessage = "La cantidad de grupos debe estar entre 1 y 30.")]
    [Display(Name = "Cantidad de grupos")] public int CantidadGrupos { get; set; } = 1;
    public List<List<EstadoFranja>> DisponibilidadesGrupos { get; set; } = [];

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
        ProgramasSeleccionados = (a.ProgramasCodigosCsv.Length > 0 ? a.ProgramasCodigosCsv.Split(',') : [a.ProgramaCodigo]).ToList(),
        Creditos = a.Creditos,
        Semestre = a.Semestre,
        IntensidadHoraria = a.IntensidadHoraria,
        Disponibilidad = [.. a.Disponibilidad],
        TipoSalonRequerido = a.TipoSalonRequerido,
        TipoAsignatura = a.TipoAsignatura,
        CantidadGrupos = a.CantidadGrupos,
        DisponibilidadesGrupos = LeerDisponibilidades(a),
        SalonesPermitidosCsv = a.SalonesPermitidosCsv
    };

    private static List<List<EstadoFranja>> LeerDisponibilidades(Asignatura a)
    {
        try
        {
            var grupos = System.Text.Json.JsonSerializer.Deserialize<List<List<EstadoFranja>>>(a.DisponibilidadesGruposJson);
            if (grupos is { Count: > 0 }) return grupos;
        }
        catch { }
        return Enumerable.Range(0, Math.Max(1, a.CantidadGrupos)).Select(_ => a.Disponibilidad.ToList()).ToList();
    }

    public Asignatura ComoEntidad() => new()
    {
        Id = Id,
        Codigo = Codigo.Trim().ToUpperInvariant(),
        CodigoCompartido = (CodigoCompartido ?? "").Trim().ToUpperInvariant(),
        Nombre = Nombre.Trim().ToUpperInvariant(),
        Tipologia = Tipologia,
        ProgramaCodigo = ProgramasSeleccionados.FirstOrDefault() ?? "",
        ProgramasCodigosCsv = string.Join(",", ProgramasSeleccionados.Distinct()),
        CantidadGrupos = Math.Clamp(CantidadGrupos, 1, 30),
        DisponibilidadesGruposJson = System.Text.Json.JsonSerializer.Serialize(
            NormalizarDisponibilidades(CantidadGrupos)),
        Creditos = Creditos,
        Semestre = string.IsNullOrWhiteSpace(Semestre) ? null : Semestre.Trim(),
        IntensidadHoraria = IntensidadHoraria,
        Disponibilidad = DisponibilidadesGrupos.FirstOrDefault()?.Count == DisponibilidadSemanal.Total
            ? [.. DisponibilidadesGrupos[0]]
            : Disponibilidad.Count == DisponibilidadSemanal.Total ? [.. Disponibilidad] : DisponibilidadSemanal.Base(),
        TipoSalonRequerido = TipoSalonRequerido,
        TipoAsignatura = TipoAsignatura,
        SalonesPermitidosCsv = SalonesPermitidosCsv?.Trim() ?? ""
    };

    private List<List<EstadoFranja>> NormalizarDisponibilidades(int cantidad)
    {
        return Enumerable.Range(0, Math.Clamp(cantidad, 1, 30)).Select(i =>
        {
            var origen = i < DisponibilidadesGrupos.Count ? DisponibilidadesGrupos[i] : Disponibilidad;
            return origen.Count == DisponibilidadSemanal.Total ? origen.ToList() : DisponibilidadSemanal.Base();
        }).ToList();
    }
}
