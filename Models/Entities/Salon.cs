namespace OfertaAcademica.Web.Models.Entities;

public class Salon
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public int Equipos { get; set; }
    public int Capacidad { get; set; }
}
