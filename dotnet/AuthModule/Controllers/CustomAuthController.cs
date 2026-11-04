using DevToolkit.Infrastructure.RateLimiting;
using DevToolkit.Auth.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DevToolkit.Auth.Constants;
using DevToolkit.Auth.DTOs.Auth;
using DevToolkit.Auth.Interfaces.Services;

namespace DevToolkit.Auth.Controllers;

[ApiController]
[ServiceFilter(typeof(AuthExceptionFilter))]
[EnableRateLimiting(RateLimitPolicies.FormSubmit)]
[Route("auth/custom")]
public class CustomAuthController : ControllerBase
{
    private readonly ICustomAuthService _authService;
    public CustomAuthController(ICustomAuthService authService)
    {
        _authService = authService;
    }
    [EnableRateLimiting(RateLimitPolicies.AuthAttempt)]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        return Ok(result);
    }

    [EnableRateLimiting(RateLimitPolicies.AuthAttempt)]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        return Ok(result);
    }
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshAsync(dto);

        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
    {
        await _authService.LogoutAsync(dto);

        return Ok(new { message = "Logged out successfully." });
    }

    [EnableRateLimiting(RateLimitPolicies.AuthAttempt)]
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto dto)
    {
        var result = await _authService.GoogleLoginAsync(dto);

        return Ok(result);
    }

    [EnableRateLimiting(RateLimitPolicies.OtpRequest)]
    [HttpPost("forgot-password/request")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestForgotPasswordOtp([FromBody] ForgotPasswordRequestDto dto)
    {
        await _authService.RequestForgotPasswordOtpAsync(dto);

        return Ok(new
        {
            message = "If an account exists for that email, a password reset code has been sent."
        });
    }

    [EnableRateLimiting(RateLimitPolicies.AuthAttempt)]
    [HttpPost("forgot-password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordRequestDto dto)
    {
        await _authService.ResetPasswordAsync(dto);

        return Ok(new
        {
            message = "Your password has been reset successfully. You can now sign in."
        });
    }

    [EnableRateLimiting(RateLimitPolicies.OtpVerify)]
    [HttpPost("forgot-password/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] VerifyForgotPasswordOtpRequestDto dto)
    {
        var response = await _authService.VerifyForgotPasswordOtpAsync(dto);

        return Ok(response);
    }
    [EnableRateLimiting(RateLimitPolicies.PublicRead)]
    [HttpGet("security-summary")]
    [Authorize(AuthenticationSchemes = AuthSchemes.CustomJwt)]
    public async Task<IActionResult> GetSecuritySummary()
    {
        var response = await _authService.GetSecuritySummaryAsync();

        return Ok(response);
    }

    [EnableRateLimiting(RateLimitPolicies.AuthAttempt)]
    [HttpPost("change-password")]
    [Authorize(AuthenticationSchemes = AuthSchemes.CustomJwt)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        await _authService.ChangePasswordAsync(dto);

        return Ok(new
        {
            message =
                "Password changed successfully. Please sign in again."
        });
    }

    [EnableRateLimiting(RateLimitPolicies.AuthAttempt)]
    [HttpPost("set-password/google-reauth")]
    [Authorize(AuthenticationSchemes = AuthSchemes.CustomJwt)]
    public async Task<IActionResult> ReauthenticateGoogleForPassword([FromBody] GooglePasswordReauthRequestDto dto)
    {
        var response = await _authService.ReauthenticateGoogleForPasswordAsync(dto);

        return Ok(response);
    }

    [EnableRateLimiting(RateLimitPolicies.AuthAttempt)]
    [HttpPost("set-password/complete")]
    [Authorize(AuthenticationSchemes = AuthSchemes.CustomJwt)]
    public async Task<IActionResult> CompleteSetPassword([FromBody] CompleteSetPasswordRequestDto dto)
    {
        await _authService.CompleteSetPasswordAsync(dto);

        return Ok(new
        {
            message = "Password created successfully. You can now sign in using Google or your password."
        });
    }


    [EnableRateLimiting(RateLimitPolicies.AuthAttempt)]
    [HttpPost("sessions/logout-others")]
    [Authorize(AuthenticationSchemes = AuthSchemes.CustomJwt)]
    public async Task<IActionResult> SignOutOtherSessions([FromBody] SignOutOtherSessionsRequestDto dto)
    {
        await _authService.SignOutOtherSessionsAsync(dto);

        return Ok(new
        {
            message = "Other devices have been signed out."
        });
    }

    [EnableRateLimiting(RateLimitPolicies.AuthAttempt)]
    [HttpPost("sessions/logout-all")]
    [Authorize(AuthenticationSchemes = AuthSchemes.CustomJwt)]
    public async Task<IActionResult> SignOutAllSessions()
    {
        await _authService.SignOutAllSessionsAsync();

        return Ok(new
        {
            message = "All devices have been signed out."
        });
    }

    [HttpGet("sessions")]
    [Authorize(AuthenticationSchemes = AuthSchemes.CustomJwt)]
    [EnableRateLimiting(RateLimitPolicies.PublicRead)]
    public async Task<IActionResult> GetActiveSessions()
    {
        var sessions = await _authService.GetActiveSessionsAsync();

        return Ok(sessions);
    }

    [EnableRateLimiting(RateLimitPolicies.AuthAttempt)]
    [HttpPost("sessions/{userSessionId:guid}/revoke")]
    [Authorize(AuthenticationSchemes = AuthSchemes.CustomJwt)]
    public async Task<IActionResult> RevokeSession(Guid userSessionId)
    {
        await _authService.RevokeSessionAsync(userSessionId);

        return Ok(new
        {
            message = "The selected device has been signed out."
        });
    }

    [HttpGet("sessions/validate")]
    [Authorize(AuthenticationSchemes = AuthSchemes.CustomJwt)]
    public IActionResult ValidateCurrentSession()
    {
        return NoContent();
    }
}
