using Microsoft.AspNetCore.Authorization;

namespace C2C.DevicePlatform.Api.Security;

public sealed class MfaRequirementHandler : AuthorizationHandler<MfaRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MfaRequirement requirement)
    {
        var amrClaims = context.User.FindAll("amr").Select(claim => claim.Value);
        var hasMfaByAmr = amrClaims.Any(value => string.Equals(value, "mfa", StringComparison.OrdinalIgnoreCase));

        var acrClaim = context.User.FindFirst("acr")?.Value;
        var hasMfaByAcr = !string.IsNullOrWhiteSpace(acrClaim)
            && acrClaim.Contains("mfa", StringComparison.OrdinalIgnoreCase);

        if (hasMfaByAmr || hasMfaByAcr)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
