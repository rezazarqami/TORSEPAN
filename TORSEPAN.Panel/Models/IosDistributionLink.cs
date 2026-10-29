namespace TORSEPAN.Panel.Models;

public static class IosDistributionLink
{
    // Accept only Apple's actual distribution hosts. Never expose arbitrary schemes or an unsigned IPA.
    public static string? Validate(string? value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment)) return null;
        var testFlight = uri.Host.Equals("testflight.apple.com", StringComparison.OrdinalIgnoreCase) &&
            System.Text.RegularExpressions.Regex.IsMatch(uri.AbsolutePath, @"^/join/[A-Za-z0-9]+/?$");
        var appStore = uri.Host.Equals("apps.apple.com", StringComparison.OrdinalIgnoreCase) &&
            System.Text.RegularExpressions.Regex.IsMatch(uri.AbsolutePath, @"/id[0-9]+/?$");
        return testFlight || appStore ? uri.AbsoluteUri : null;
    }
}
