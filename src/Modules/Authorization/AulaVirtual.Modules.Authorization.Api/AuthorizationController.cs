using Asp.Versioning;
using AulaVirtual.Modules.Authorization.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AulaVirtual.Modules.Authorization.Api;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/authorization")]
[Authorize]
public sealed class AuthorizationController(AuthorizationUseCases useCases) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("user_id")?.Value, out var userId) || userId == Guid.Empty)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized.");
        var result = await useCases.GetEffectiveAsync(userId, cancellationToken);
        return Ok(new AuthorizationMeResponse(result.Roles, result.Permissions));
    }
}

public sealed record AuthorizationMeResponse(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
