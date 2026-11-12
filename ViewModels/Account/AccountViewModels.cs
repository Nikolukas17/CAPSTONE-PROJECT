using System.ComponentModel.DataAnnotations;

namespace OfertaAcademica.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo institucional")][EmailAddress(ErrorMessage = "El correo no es válido")]
    [Display(Name = "Correo institucional")] public string Correo { get; set; } = "";
    [Required(ErrorMessage = "Ingresa tu contraseña")][DataType(DataType.Password)]
    [Display(Name = "Contraseña")] public string Contrasena { get; set; } = "";
}

public class RegistroViewModel
{
    [Required(ErrorMessage = "Ingresa tu nombre completo")][Display(Name = "Nombre completo")] public string NombreCompleto { get; set; } = "";
    [Required(ErrorMessage = "Ingresa tu cargo")] public string Cargo { get; set; } = "";
    [Required(ErrorMessage = "Ingresa tu correo institucional")][EmailAddress(ErrorMessage = "El correo no es válido")]
    [RegularExpression(@"(?i)^[^@\s]+@urosario\.edu\.co$", ErrorMessage = "Debes utilizar un correo @urosario.edu.co")]
    [Display(Name = "Correo institucional")] public string Correo { get; set; } = "";
    [Required(ErrorMessage = "Crea una contraseña")][MinLength(10, ErrorMessage = "Usa al menos 10 caracteres")]
    [DataType(DataType.Password)] public string Contrasena { get; set; } = "";
    [Required][Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden")][DataType(DataType.Password)] public string ConfirmarContrasena { get; set; } = "";
}

public class OlvideViewModel
{
    [Required][EmailAddress][RegularExpression(@"(?i)^[^@\s]+@urosario\.edu\.co$", ErrorMessage = "Debes utilizar un correo @urosario.edu.co")]
    [Display(Name = "Correo institucional")] public string Correo { get; set; } = "";
}

public class RestablecerPasswordViewModel
{
    [Required] public string Token { get; set; } = "";
    [Required][EmailAddress] public string Correo { get; set; } = "";
    [Required][MinLength(10)][DataType(DataType.Password)] public string NuevaContrasena { get; set; } = "";
    [Required][Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden")][DataType(DataType.Password)] public string ConfirmarContrasena { get; set; } = "";
}
