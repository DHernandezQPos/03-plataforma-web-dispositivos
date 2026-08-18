using Microsoft.AspNetCore.Authorization;

namespace C2C.DevicePlatform.Api.Security;

public sealed class EnvironmentAccessRequirement : IAuthorizationRequirement
{
    public EnvironmentAccessRequirement(params string[] allowedEnvironments)
    {
        AllowedEnvironments = allowedEnvironments;
    }

    public IReadOnlyCollection<string> AllowedEnvironments { get; }
}
