using System.ComponentModel.DataAnnotations;
using DevToolkit.Auth.Helpers;

namespace DevToolkit.Auth.DTOs;

public class LogoutRequestDto
{
    [Required, MaxLength(512)]
    public string RefreshToken { get; set; } = string.Empty;
}
