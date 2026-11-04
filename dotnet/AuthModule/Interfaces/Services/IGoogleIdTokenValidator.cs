using Google.Apis.Auth;

namespace DevToolkit.Auth.Interfaces.Services;

public interface IGoogleIdTokenValidator
{
    Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string clientId);
}
