using System.ComponentModel.DataAnnotations;
using DevToolkit.Auth.Helpers;

namespace DevToolkit.Auth.DTOs;

public class ResetPasswordRequestDto
{
    [Required, MaxLength(512)]
    public string ResetToken { get; set; } = string.Empty;
    [Required, MinLength(8), MaxLength(72), Utf8MaxLength(72)]
    public string NewPassword { get; set; } = string.Empty;
}
