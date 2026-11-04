using System.ComponentModel.DataAnnotations;
using DevToolkit.Auth.Helpers;

namespace DevToolkit.Auth.DTOs;

public class LoginRequestDto
{
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(72), Utf8MaxLength(72)]
    public string Password { get; set; } = string.Empty;
}
