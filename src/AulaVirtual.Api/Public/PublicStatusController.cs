using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AulaVirtual.Api.Public;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/public")]
[AllowAnonymous]
[EnableRateLimiting(PublicApiRateLimiting.PolicyName)]
public sealed class PublicStatusController : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<PublicStatusResponse> GetStatus(
        [FromQuery] PublicStatusRequest request)
    {
        return Ok(new PublicStatusResponse("available", request.CorrelationId));
    }
}

public sealed class PublicStatusRequest
{
    [RegularExpression(
        "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    public string? CorrelationId { get; init; }
}

public sealed record PublicStatusResponse(string Status, string? CorrelationId);
