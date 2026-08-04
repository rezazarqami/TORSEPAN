using System.Security.Claims;
using System.Text.Json;

namespace TORSEPAN.Panel.Authentication;

public static class JwtParser
{
    public static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var claims = new List<Claim>();

        if (string.IsNullOrWhiteSpace(jwt))
            return claims;

        var parts = jwt.Split('.');

        if (parts.Length != 3)
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

        var jsonBytes = Convert.FromBase64String(payload);

        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);

        if (values is null)
            return claims;

        foreach (var item in values)
        {
            if (item.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in item.Value.EnumerateArray())
                {
                    claims.Add(new Claim(item.Key, role.GetString() ?? string.Empty));
                }

                continue;
            }

            claims.Add(new Claim(item.Key, item.Value.ToString()));
        }

        return claims;
    }
}