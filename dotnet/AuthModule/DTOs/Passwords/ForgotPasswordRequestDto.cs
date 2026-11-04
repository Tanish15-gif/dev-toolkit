using System.ComponentModel.DataAnnotations;
using DevToolkit.Infrastructure.Helpers;

namespace DevToolkit.Auth.DTOs.Auth;

public class ForgotPasswordRequestDto
{
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}
