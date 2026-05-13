using System.Diagnostics;
using System.Net;
using System.Net.Mail;
namespace ClimaOS_Desktop.Services;
public class EmailService
{
    private readonly EmailSettings _settings;
    public EmailService(EmailSettings settings)
    {
        _settings = settings;
    }
    public async Task SendResetCodeAsync(string toEmail, string code, int expiresInMinutes, CancellationToken ct = default)
    {
        var subject = "Cod resetare parolă ClimaOS";
        var body = $"""
                    Salut,
                    Codul tău de resetare a parolei este: {code}
                    Codul expiră în {expiresInMinutes} minute.
                    Dacă nu ai cerut această resetare, poți ignora acest mesaj.
                    Echipa ClimaOS
                    """;
        if (!_settings.UseConfiguredSmtp)
        {
            if (_settings.LooksLikePlaceholderConfiguration)
            {
                throw new InvalidOperationException(
                    "Emailul nu este configurat încă. Completează în smtp.settings.local.json adresa Gmail reală, parola de aplicație și adresa expeditorului.");
            }
            Debug.WriteLine("======================================");
            Debug.WriteLine($"[EMAIL FALLBACK] To: {toEmail}");
            Debug.WriteLine($"[EMAIL FALLBACK] Subject: {subject}");
            Debug.WriteLine($"[EMAIL FALLBACK] Body: {body}");
            Debug.WriteLine("======================================");
            await Task.Delay(150, ct);
            return;
        }
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(toEmail);
        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }
        try
        {
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Trimiterea emailului de resetare a eșuat. Verifică setările SMTP ale aplicației.",
                ex);
        }
    }
}
