using System.Security.Claims;

namespace C2C.DevicePlatform.Api.Security;

public sealed class UserEnvironmentScopeService
{
    public bool CanAccessEnvironment(ClaimsPrincipal user, string environment)
    {
        var requestedEnvironment = (environment ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(requestedEnvironment))
        {
            return false;
        }

        return GetAllowedEnvironments(user)
            .Contains(requestedEnvironment, StringComparer.OrdinalIgnoreCase);
    }

    public HashSet<string> GetAllowedEnvironments(ClaimsPrincipal user)
    {
        var values = user.FindAll("env").Select(claim => claim.Value)
            .Concat(user.FindAll("environment").Select(claim => claim.Value))
            .SelectMany(SplitEnvironments)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToLowerInvariant());

        return values.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public string GetActor(ClaimsPrincipal user)
    {
        var subject = user.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(subject))
        {
            return subject;
        }

        var email = user.FindFirst("email")?.Value;
        if (!string.IsNullOrWhiteSpace(email))
        {
            return email;
        }

        var name = user.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return "unknown";
    }

    private static IEnumerable<string> SplitEnvironments(string claimValue)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return [];
        }

        return claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
