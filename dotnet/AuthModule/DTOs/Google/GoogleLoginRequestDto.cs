using System.ComponentModel.DataAnnotations;
using DevToolkit.Infrastructure.Helpers;

namespace DevToolkit.Auth.DTOs.Auth;

public class GoogleLoginRequestDto
{
    [Required, MaxLength(8192)]
    public string IdToken { get; set; } = string.Empty;
}
