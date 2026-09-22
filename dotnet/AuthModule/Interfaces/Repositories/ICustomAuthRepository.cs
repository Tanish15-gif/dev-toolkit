using AuthModule.Entities;

namespace AuthModule.Interfaces.Repositories;

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
