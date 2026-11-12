using System.Net;
using System.Net.Mail;

namespace OfertaAcademica.Web.Services;

public interface ICorreoInstitucionalService
{
    Task EnviarRecuperacionAsync(string destino, string nombre, string enlace, CancellationToken cancellationToken = default);
}

public sealed class CorreoInstitucionalService(IConfiguration configuration, IWebHostEnvironment environment) : ICorreoInstitucionalService
{
    public async Task EnviarRecuperacionAsync(string destino, string nombre, string enlace, CancellationToken cancellationToken = default)
    {
        var section = configuration.GetSection("Email");
        var host = section["SmtpHost"];
        var from = section["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("El envío de recuperación aún no está configurado. Completa Email:SmtpHost y Email:From mediante variables de entorno o configuración segura.");

        using var message = new MailMessage(from, destino)
        {
            Subject = "Recuperación de contraseña – Gestión de Oferta Académica",
            IsBodyHtml = true,
            Body = $"<p>Hola {WebUtility.HtmlEncode(nombre)},</p><p>Recibimos una solicitud para cambiar la contraseña de tu cuenta.</p><p><a href=\"{WebUtility.HtmlEncode(enlace)}\">Restablecer contraseña</a></p><p>El enlace vence en 30 minutos. Si no solicitaste este cambio, ignora este mensaje.</p>"
        };
        using var client = new SmtpClient(host, section.GetValue<int?>("Port") ?? 587)
        {
            EnableSsl = section.GetValue<bool?>("EnableSsl") ?? true,
            Credentials = string.IsNullOrWhiteSpace(section["Username"]) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(section["Username"], section["Password"])
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
