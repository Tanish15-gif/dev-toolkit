namespace DevToolkit.Auth.Models;

/// <summary>Parsed result from a raw HTTP User-Agent string.</summary>
/// <param name="DeviceType">Desktop | Mobile | Tablet | TV</param>
/// <param name="DeviceName">e.g. "Windows PC", "iPhone", "iPad"</param>
/// <param name="BrowserName">e.g. "Chrome", "Safari", "Firefox"</param>
/// <param name="BrowserVersion">e.g. "120.0" (nullable)</param>
/// <param name="OperatingSystem">e.g. "Windows", "iOS", "Android"</param>
public record UserAgentDetails(
    string DeviceType,
    string DeviceName,
    string BrowserName,
    string? BrowserVersion,
    string OperatingSystem
);
