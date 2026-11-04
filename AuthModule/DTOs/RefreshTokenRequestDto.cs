using System.ComponentModel.DataAnnotations;
using DevToolkit.Auth.Helpers;

namespace DevToolkit.Auth.DTOs;

public class RefreshTokenRequestDto
{
    [Required, MaxLength(512)]
    public string RefreshToken { get; set; } = string.Empty;
}
