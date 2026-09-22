using AuthModule.Models;

namespace AuthModule.Interfaces.Services;

public interface IUserAgentParser
{
    UserAgentDetails Parse(string? userAgent);
}