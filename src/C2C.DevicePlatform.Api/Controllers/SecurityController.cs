using C2C.DevicePlatform.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C2C.DevicePlatform.Api.Controllers;

[ApiController]
[Route("api/security")]
public sealed class SecurityController : ControllerBase
{
    [HttpGet("mfa-check")]
    [Authorize(Policy = PolicyNames.AdminMfa)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> CheckAdminMfa()
    {
        return Ok(new
        {
            Message = "MFA validated for admin operation.",
            CheckedAtUtc = DateTimeOffset.UtcNow
        });
    }
}
