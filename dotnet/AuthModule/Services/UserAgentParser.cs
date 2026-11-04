using System.Text.RegularExpressions;
using DevToolkit.Auth.Interfaces.Services;
using DevToolkit.Auth.Models;

namespace DevToolkit.Auth.Services;

public class UserAgentParser : IUserAgentParser
{
    public UserAgentDetails Parse(string? userAgent)
    {
        var ua = userAgent ?? string.Empty;

        var (browserName, browserVersion) = GetBrowser(ua);

        var operatingSystem = GetOperatingSystem(ua);

        var deviceType = GetDeviceType(ua);

        var deviceName = GetDeviceName(deviceType, operatingSystem);

        return new UserAgentDetails(
            deviceType,
            deviceName,
            browserName,
            browserVersion,
            operatingSystem
        );
    }

    private static (string Name, string? Version) GetBrowser(string ua)
    {
        if (ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) return ("Edge", ExtractVersion(ua, "Edg/"));
        if (ua.Contains("OPR/", StringComparison.OrdinalIgnoreCase)) return ("Opera", ExtractVersion(ua, "OPR/"));
        if (ua.Contains("SamsungBrowser/", StringComparison.OrdinalIgnoreCase)) return ("Samsung Internet", ExtractVersion(ua, "SamsungBrowser/"));
        if (ua.Contains("CriOS/", StringComparison.OrdinalIgnoreCase)) return ("Chrome", ExtractVersion(ua, "CriOS/"));
        if (ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase)) return ("Chrome", ExtractVersion(ua, "Chrome/"));
        if (ua.Contains("FxiOS/", StringComparison.OrdinalIgnoreCase)) return ("Firefox", ExtractVersion(ua, "FxiOS/"));
        if (ua.Contains("Firefox/", StringComparison.OrdinalIgnoreCase)) return ("Firefox", ExtractVersion(ua, "Firefox/"));
        if (
            ua.Contains("Safari/", StringComparison.OrdinalIgnoreCase) &&
            ua.Contains("Version", StringComparison.OrdinalIgnoreCase)
        ) return ("Safari", ExtractVersion(ua, "Version/"));

        return ("Unknown browser", null);
    }

    private static string GetOperatingSystem(string ua)
    {
        if (ua.Contains("Tizen", StringComparison.OrdinalIgnoreCase)) return "Tizen";
        if (ua.Contains("Web0S", StringComparison.OrdinalIgnoreCase) || ua.Contains("WebOS", StringComparison.OrdinalIgnoreCase)) return "webOS";
        if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)) return "iOS";
        if (ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)) return "iPadOS";
        if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase)) return "Android";
        if (ua.Contains("Windows NT", StringComparison.OrdinalIgnoreCase)) return "Windows";
        if (ua.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase)) return "macOS";
        if (ua.Contains("CrOS", StringComparison.OrdinalIgnoreCase)) return "ChromeOS";
        if (ua.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "Linux";
        return "Unknown OS";
    }

    private static string GetDeviceType(string ua)
    {
        if (
            ua.Contains("SMART-TV", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("SmartTV", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("HbbTV", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Tizen", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Web0S", StringComparison.OrdinalIgnoreCase)
        )
        {
            return "TV";
        }

        if (
            ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Tablet", StringComparison.OrdinalIgnoreCase)
        )
        {
            return "Tablet";
        }

        if (
            ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
        )
        {
            return "Mobile";
        }

        return "Desktop";
    }

    private static string GetDeviceName(string deviceType, string operatingSystem)
    {
        return (deviceType, operatingSystem) switch
        {
            ("TV", _) => "Smart TV",

            ("Mobile", "iOS") => "iPhone",
            ("Mobile", "Android") => "Android phone",

            ("Tablet", "iPadOS") => "iPad",
            ("Tablet", "Android") => "Android tablet",

            ("Desktop", "Windows") => "Windows PC",
            ("Desktop", "macOS") => "Mac",
            ("Desktop", "ChromeOS") => "Chromebook",
            ("Desktop", "Linux") => "Linux computer",

            _ => "Unknown device"
        };
    }
    private static string? ExtractVersion(string userAgent, string marker)
    {
        var match = Regex.Match(
            userAgent,
            $"{Regex.Escape(marker)}(?<version>[0-9.]+)",
            RegexOptions.IgnoreCase
        );

        return match.Success ? match.Groups["version"].Value : null;
    }
}
