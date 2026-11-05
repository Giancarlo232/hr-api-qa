using System.Net.Mail;

namespace HrApi;

public static class Validators
{
    /// <summary>Correo válido: parseable y sin nombre para mostrar.</summary>
    public static bool Email(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var addr = new MailAddress(email.Trim());
            return addr.Address == email.Trim() && addr.Host.Contains('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
