using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DevToolkit.Auth.Helpers;

public sealed class Utf8MaxLengthAttribute(int maxBytes) : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null ||
        value is string text && Encoding.UTF8.GetByteCount(text) <= maxBytes;
    public override string FormatErrorMessage(string name) => $"{name} must not exceed {maxBytes} UTF-8 bytes.";
}

public sealed class OptionalEmailAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null || value is string text &&
        (string.IsNullOrWhiteSpace(text) || new EmailAddressAttribute().IsValid(text));
}
