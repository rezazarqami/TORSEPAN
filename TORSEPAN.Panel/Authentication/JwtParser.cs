using System.Security.Claims;
using System.Text.Json;

namespace TORSEPAN.Panel.Authentication;

public static class JwtParser
{
    public static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var claims = new List<Claim>();

        var parts = jwt.Split('.');

        if (parts.Length < 2)
            return claims;

        var payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        switch (payload.Length % 4)
        {
            case 2:
                payload += "==";
                break;
            case 3:
                payload += "=";
                break;
        }

        var json = Convert.FromBase64String(payload);

        var values = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        if (values is null)
            return claims;

        foreach (var item in values)
        {
            claims.Add(new Claim(item.Key, item.Value?.ToString() ?? ""));
        }

        return claims;
    }
}