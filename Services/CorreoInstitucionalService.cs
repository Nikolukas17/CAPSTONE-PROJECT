using System.Net;
using System.Net.Mail;

namespace OfertaAcademica.Web.Services;

public enum ResultadoEnvioCorreo
{
    /// <summary>El correo salió por el servidor SMTP configurado.</summary>
    Enviado,
    /// <summary>Modo desarrollo sin SMTP: el correo se guardó como archivo en App_Data/correos-desarrollo.</summary>
    GuardadoLocalmente
}

public interface ICorreoInstitucionalService
{
    bool SmtpConfigurado { get; }
    Task<ResultadoEnvioCorreo> EnviarRecuperacionAsync(string destino, string nombre, string enlace, CancellationToken cancellationToken = default);
    Task<ResultadoEnvioCorreo> EnviarEnlaceDisponibilidadAsync(string destino, string nombre, string enlace, DateTime venceUtc, CancellationToken cancellationToken = default);
}

/// <summary>
/// Envío de correos por SMTP (sección "Email" de la configuración; p. ej. Gmail con contraseña de aplicación).
/// En desarrollo, si no hay SMTP configurado, el mensaje se guarda como .html en App_Data/correos-desarrollo
/// y el enlace se escribe en el log, para poder probar los flujos sin enviar correos reales.
/// En producción, sin SMTP, se lanza una excepción: nunca se simula un envío exitoso.
/// </summary>
public sealed class CorreoInstitucionalService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<CorreoInstitucionalService> logger) : ICorreoInstitucionalService
{
    private IConfigurationSection Seccion => configuration.GetSection("Email");

    public bool SmtpConfigurado =>
        !string.IsNullOrWhiteSpace(Seccion["SmtpHost"]) && !string.IsNullOrWhiteSpace(Seccion["From"]);

    public Task<ResultadoEnvioCorreo> EnviarRecuperacionAsync(string destino, string nombre, string enlace, CancellationToken cancellationToken = default) =>
        EnviarAsync(destino, "Recuperación de contraseña – Gestión de Oferta Académica",
            $"""
            <p>Hola {WebUtility.HtmlEncode(nombre)},</p>
            <p>Recibimos una solicitud para cambiar la contraseña de tu cuenta.</p>
            <p><a href="{WebUtility.HtmlEncode(enlace)}">Restablecer contraseña</a></p>
            <p>El enlace vence en 30 minutos. Si no solicitaste este cambio, ignora este mensaje.</p>
            """, enlace, cancellationToken);

    public Task<ResultadoEnvioCorreo> EnviarEnlaceDisponibilidadAsync(string destino, string nombre, string enlace, DateTime venceUtc, CancellationToken cancellationToken = default) =>
        EnviarAsync(destino, "Registra tu disponibilidad horaria – Escuela de Ciencias e Ingeniería",
            $"""
            <p>Hola {WebUtility.HtmlEncode(nombre)},</p>
            <p>La coordinación académica está preparando la oferta del próximo periodo. Por favor marca en qué días y horas puedes dictar clase:</p>
            <p><a href="{WebUtility.HtmlEncode(enlace)}">Registrar mi disponibilidad</a></p>
            <p>El enlace es personal y vence el {venceUtc.ToLocalTime():dd/MM/yyyy}. Puedes volver a abrirlo para corregir tu disponibilidad mientras esté vigente.</p>
            """, enlace, cancellationToken);

    private async Task<ResultadoEnvioCorreo> EnviarAsync(string destino, string asunto, string cuerpoHtml, string enlace, CancellationToken cancellationToken)
    {
        if (!SmtpConfigurado)
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("El envío de correos no está configurado. Completa Email:SmtpHost y Email:From (ver CONFIGURACION_PRODUCCION.md).");
            return await GuardarLocalmenteAsync(destino, asunto, cuerpoHtml, enlace, cancellationToken);
        }

        var seccion = Seccion;
        using var message = new MailMessage(seccion["From"]!, destino)
        {
            Subject = asunto,
            IsBodyHtml = true,
            Body = cuerpoHtml
        };
        if (!string.IsNullOrWhiteSpace(seccion["FromName"]))
            message.From = new MailAddress(seccion["From"]!, seccion["FromName"]);

        using var client = new SmtpClient(seccion["SmtpHost"], seccion.GetValue<int?>("Port") ?? 587)
        {
            EnableSsl = seccion.GetValue<bool?>("EnableSsl") ?? true,
            UseDefaultCredentials = false,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrWhiteSpace(seccion["Username"]))
            client.Credentials = new NetworkCredential(seccion["Username"], seccion["Password"]);

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Correo '{Asunto}' enviado a {Destino}.", asunto, destino);
        return ResultadoEnvioCorreo.Enviado;
    }

    private async Task<ResultadoEnvioCorreo> GuardarLocalmenteAsync(string destino, string asunto, string cuerpoHtml, string enlace, CancellationToken cancellationToken)
    {
        var carpeta = Path.Combine(environment.ContentRootPath, "App_Data", "correos-desarrollo");
        Directory.CreateDirectory(carpeta);
        var archivo = Path.Combine(carpeta, $"{DateTime.Now:yyyyMMdd-HHmmss-fff}.html");
        var html = $"""
            <!doctype html><meta charset="utf-8"><title>{WebUtility.HtmlEncode(asunto)}</title>
            <p><b>Para:</b> {WebUtility.HtmlEncode(destino)}<br><b>Asunto:</b> {WebUtility.HtmlEncode(asunto)}</p><hr>
            {cuerpoHtml}
            """;
        await File.WriteAllTextAsync(archivo, html, cancellationToken);
        logger.LogWarning("SMTP no configurado (modo desarrollo). Correo para {Destino} guardado en {Archivo}. Enlace: {Enlace}", destino, archivo, enlace);
        return ResultadoEnvioCorreo.GuardadoLocalmente;
    }
}
