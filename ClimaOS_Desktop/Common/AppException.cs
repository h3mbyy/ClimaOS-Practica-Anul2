namespace ClimaOS_Desktop.Common;
public class AppException : Exception
{
    public string Title { get; }
    public string FriendlyMessage { get; }
    public AppException(string friendlyMessage, string title = "Eroare", Exception? inner = null)
        : base(friendlyMessage, inner)
    {
        Title = title;
        FriendlyMessage = friendlyMessage;
    }
}
public sealed class ValidationException : AppException
{
    public IReadOnlyList<string> Errors { get; }
    public ValidationException(IEnumerable<string> errors)
        : base(string.Join("\n", errors), "Date invalide")
    {
        Errors = errors.ToArray();
    }
    public ValidationException(string message)
        : this(new[] { message })
    {
    }
}
public sealed class AuthException : AppException
{
    public AuthException(string message) : base(message, "Autentificare") { }
}
public sealed class DatabaseException : AppException
{
    public DatabaseException(string message, Exception? inner = null)
        : base(message, "Bază de date", inner) { }
}
