using DevToolkit.Auth.Entities;

namespace DevToolkit.Auth.Interfaces.Repositories;

/// <summary>
/// Composite repository interface used by auth services.
/// Aggregates methods from all individual repository interfaces.
/// </summary>
public interface ICustomAuthRepository :
    ICredentialAuthRepository,
    IGoogleAuthRepository,
    IPasswordRecoveryRepository,
    ISessionRepository,
    ITokenRepository
{
}
