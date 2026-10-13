using Asp.Versioning;
using AulaVirtual.Modules.Courses.Application.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AulaVirtual.Modules.Courses.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/public/courses")]
[AllowAnonymous]
[EnableRateLimiting("public-api")]
public sealed class PublicCoursesController(PublicCourseCatalogUseCases useCases) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await useCases.ListAsync(cancellationToken));
}
