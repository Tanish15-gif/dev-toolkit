namespace DevToolkit.Auth.Exceptions;

public class AuthConflictException : ConflictException
{
    public AuthConflictException(string message) : base(message)
    {
    }
}
