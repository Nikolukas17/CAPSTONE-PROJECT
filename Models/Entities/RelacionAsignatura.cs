namespace OfertaAcademica.Web.Models.Entities;

public enum TipoRelacionAsignatura
{
    Prerrequisito = 0,
    Correquisito = 1
}

public class RelacionAsignatura
{
    public int Id { get; set; }
    public int AsignaturaId { get; set; }
    public int RelacionadaId { get; set; }
    public TipoRelacionAsignatura Tipo { get; set; }
}
