using System.ComponentModel.DataAnnotations;

namespace OfertaAcademica.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo institucional")]
    [EmailAddress(ErrorMessage = "El correo no es válido")]
    [Display(Name = "Correo institucional")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu contraseña")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = "";
}

public class RegistroViewModel
{
    [Required(ErrorMessage = "Ingresa tu nombre completo")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu cargo")]
    public string Cargo { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu correo institucional")]
    [EmailAddress(ErrorMessage = "El correo no es válido")]
    [Display(Name = "Correo institucional")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Crea una contraseña")]
    [MinLength(8, ErrorMessage = "Usa al menos 8 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = "";
}

public class OlvideViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo institucional")]
    [EmailAddress(ErrorMessage = "El correo no es válido")]
    [Display(Name = "Correo institucional")]
    public string Correo { get; set; } = "";
}