namespace OfertaAcademica.Web.ViewModels.Dashboard;

public class DashboardViewModel
{
    public string NombreUsuario { get; set; } = "";
    public List<PasoViewModel> Pasos { get; set; } = new();
}

public record PasoViewModel(string Icono, string Tono, string Titulo, string Descripcion);
