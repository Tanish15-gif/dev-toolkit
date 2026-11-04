using System.ComponentModel.DataAnnotations;

namespace DevToolkit.Auth.DTOs.Auth;

public class RegisterRequestDto
{
    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    [Required, MinLength(8), MaxLength(72), DevToolkit.Infrastructure.Helpers.Utf8MaxLength(72)]
    public string Password { get; set; } = string.Empty;
}
