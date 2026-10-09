using System.ComponentModel.DataAnnotations;

namespace OfertaAcademica.Web.ViewModels.Account;

public static class ReglasCuenta
{
    public const string PatronCorreo = @"^[A-Za-z0-9._%+-]+@urosario\.edu\.co$";
    public const string MensajeCorreo = "Usa tu correo institucional (@urosario.edu.co).";
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Escribe tu correo institucional.")]
    [RegularExpression(ReglasCuenta.PatronCorreo, ErrorMessage = ReglasCuenta.MensajeCorreo)]
    [Display(Name = "Correo institucional")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = "";
}

public class RegistroViewModel
{
    [Required(ErrorMessage = "Escribe tu nombre completo.")]
    [StringLength(100)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu cargo.")]
    [StringLength(80)]
    [Display(Name = "Cargo")]
    public string Cargo { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu correo institucional.")]
    [RegularExpression(ReglasCuenta.PatronCorreo, ErrorMessage = ReglasCuenta.MensajeCorreo)]
    [Display(Name = "Correo institucional")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Crea una contraseña.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = "";
}

public class OlvideViewModel
{
    [Required(ErrorMessage = "Escribe tu correo institucional.")]
    [RegularExpression(ReglasCuenta.PatronCorreo, ErrorMessage = ReglasCuenta.MensajeCorreo)]
    [Display(Name = "Correo institucional")]
    public string Correo { get; set; } = "";
}
