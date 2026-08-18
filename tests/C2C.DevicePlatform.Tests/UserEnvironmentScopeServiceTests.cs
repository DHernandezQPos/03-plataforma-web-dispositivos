using C2C.DevicePlatform.Api.Security;
using System.Security.Claims;

namespace C2C.DevicePlatform.Tests;

public sealed class UserEnvironmentScopeServiceTests
{
    [Fact]
    public void CanAccessEnvironment_HandlesCommaSeparatedClaims()
    {
        var service = new UserEnvironmentScopeService();
        var principal = BuildPrincipal(new Claim("environment", "demo,qa"));

        Assert.True(service.CanAccessEnvironment(principal, "demo"));
        Assert.True(service.CanAccessEnvironment(principal, "qa"));
        Assert.False(service.CanAccessEnvironment(principal, "prod"));
    }

    [Fact]
    public void GetActor_PrefersSubjectClaim()
    {
        var service = new UserEnvironmentScopeService();
        var principal = BuildPrincipal(
            new Claim("sub", "subject-user"),
            new Claim("email", "user@example.com"));

        var actor = service.GetActor(principal);
        Assert.Equal("subject-user", actor);
    }

    private static ClaimsPrincipal BuildPrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }
}
