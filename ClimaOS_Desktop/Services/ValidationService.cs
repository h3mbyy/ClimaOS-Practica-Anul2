using System.Text.RegularExpressions;
using ClimaOS_Desktop.Common;

namespace ClimaOS_Desktop.Services;

public static class ValidationService
{
    private static readonly Regex EmailRegex =
        new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    public static List<string> ValidateEmail(string? email)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("Emailul este obligatoriu.");
        }
        else if (!EmailRegex.IsMatch(email.Trim()))
        {
            errors.Add("Emailul nu pare valid.");
        }
        return errors;
    }

    public static List<string> ValidatePassword(string? password)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Parola este obligatorie.");
        }
        else if (password.Length < 6)
        {
            errors.Add("Parola trebuie să aibă cel puțin 6 caractere.");
        }
        return errors;
    }

    public static List<string> ValidateRequired(string? value, string fieldLabel)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldLabel} este obligatoriu.");
        }
        return errors;
    }

    public static void EnsureValid(IEnumerable<string> errors)
    {
        var list = errors.Where(e => !string.IsNullOrEmpty(e)).ToList();
        if (list.Count > 0)
            throw new ValidationException(list);
    }
}
