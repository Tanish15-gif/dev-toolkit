using System.ComponentModel.DataAnnotations;
using DevToolkit.Infrastructure.Helpers;

namespace DevToolkit.Auth.DTOs.Auth;

public class RefreshTokenRequestDto
{
    [Required, MaxLength(512)]
    public string RefreshToken { get; set; } = string.Empty;
}
