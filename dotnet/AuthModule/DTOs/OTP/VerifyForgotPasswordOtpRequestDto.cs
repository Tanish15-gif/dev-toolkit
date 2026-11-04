using System.ComponentModel.DataAnnotations;
using DevToolkit.Infrastructure.Helpers;

namespace DevToolkit.Auth.DTOs.Auth;

public class VerifyForgotPasswordOtpRequestDto
{
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;
    [Required, RegularExpression("^[0-9]{6}$")]
    public string Code { get; set; } = string.Empty;
}
