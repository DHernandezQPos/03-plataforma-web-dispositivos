using Microsoft.AspNetCore.Authorization;

namespace C2C.DevicePlatform.Api.Security;

public sealed class EnvironmentAccessHandler : AuthorizationHandler<EnvironmentAccessRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EnvironmentAccessRequirement requirement)
    {
        var envClaim = context.User.FindFirst("env")?.Value
            ?? context.User.FindFirst("environment")?.Value;

        if (!string.IsNullOrWhiteSpace(envClaim)
            && requirement.AllowedEnvironments.Any(allowed => string.Equals(allowed, envClaim, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
