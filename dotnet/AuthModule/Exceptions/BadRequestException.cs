namespace AuthModule.Exceptions;

/// <summary>Thrown when the request is semantically invalid (HTTP 400).</summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
    public BadRequestException(string message, Exception inner) : base(message, inner) { }
}
