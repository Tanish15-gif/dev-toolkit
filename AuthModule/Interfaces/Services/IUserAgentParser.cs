using DevToolkit.Auth.Models;

namespace DevToolkit.Auth.Interfaces.Services;

public interface IUserAgentParser
{
    UserAgentDetails Parse(string? userAgent);
}
