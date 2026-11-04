using Google.Apis.Auth;
using DevToolkit.Auth.Interfaces.Services;

namespace DevToolkit.Auth.Services;

public sealed class GoogleIdTokenValidator : IGoogleIdTokenValidator
{
    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string clientId)
        => GoogleJsonWebSignature.ValidateAsync(idToken,
            new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
}
