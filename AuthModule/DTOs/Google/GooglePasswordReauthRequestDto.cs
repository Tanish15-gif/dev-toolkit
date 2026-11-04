using System.ComponentModel.DataAnnotations;
using DevToolkit.Auth.Helpers;

namespace DevToolkit.Auth.DTOs;

public class GooglePasswordReauthRequestDto
{
    [Required, MaxLength(8192)]
    public string IdToken { get; set; } = string.Empty;
}
