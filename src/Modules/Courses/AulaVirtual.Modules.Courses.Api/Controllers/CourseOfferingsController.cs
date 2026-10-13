using Asp.Versioning;
using AulaVirtual.Modules.Authorization.Application;
using AulaVirtual.Modules.Courses.Api.Contracts;
using AulaVirtual.Modules.Courses.Application.Offerings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AulaVirtual.Modules.Courses.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/courses/{courseId:guid}/offerings")]
[Authorize(Policy = Permissions.CourseOfferingsManage)]
public sealed class CourseOfferingsController(CourseOfferingUseCases useCases) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(Guid courseId, SaveCourseOfferingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.CreateCourseOfferingAsync(
                new CreateCourseOfferingCommand(courseId, request.Name, request.MaximumCapacity,
                    request.BasePrice, request.CurrencyCode, request.PlannedSessionCount,
                    request.Sessions.Select(session => session.ToPlan()).ToArray()), cancellationToken);
            return result is null ? NotFound() : CreatedAtAction(nameof(Get),
                new { version = "1", courseId, offeringId = result.Id }, CourseOfferingResponse.From(result));
        }
        catch (ArgumentException exception) { return BadRequestProblem(exception.Message); }
    }

    [HttpPut("{offeringId:guid}")]
    public async Task<IActionResult> Update(Guid courseId, Guid offeringId, SaveCourseOfferingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.UpdateCourseOfferingAsync(
                new UpdateCourseOfferingCommand(courseId, offeringId, request.Name,
                    request.MaximumCapacity, request.BasePrice, request.CurrencyCode, request.PlannedSessionCount,
                    request.Sessions.Select(session => session.ToPlan()).ToArray()), cancellationToken);
            return result is null ? NotFound() : Ok(CourseOfferingResponse.From(result));
        }
        catch (ArgumentException exception) { return BadRequestProblem(exception.Message); }
        catch (InvalidOperationException exception) { return ConflictProblem(exception.Message); }
    }

    [HttpGet("{offeringId:guid}")]
    public async Task<IActionResult> Get(Guid courseId, Guid offeringId, CancellationToken cancellationToken)
    {
        var result = await useCases.GetCourseOfferingAsync(courseId, offeringId, cancellationToken);
        return result is null ? NotFound() : Ok(CourseOfferingResponse.From(result));
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid courseId, CancellationToken cancellationToken)
    {
        var result = await useCases.ListCourseOfferingsByCourseAsync(courseId, cancellationToken);
        return result is null ? NotFound() : Ok(result.Select(CourseOfferingResponse.From));
    }

    [HttpPost("{offeringId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid courseId, Guid offeringId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.PublishCourseOfferingAsync(courseId, offeringId, cancellationToken);
            return result is null ? NotFound() : Ok(CourseOfferingResponse.From(result));
        }
        catch (InvalidOperationException exception) { return ConflictProblem(exception.Message); }
    }

    [HttpPatch("{offeringId:guid}/sessions/{sessionId:guid}/virtual-link")]
    public async Task<IActionResult> UpdateVirtualLink(Guid courseId, Guid offeringId, Guid sessionId,
        UpdateVirtualLinkRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.UpdateSessionVirtualLinkAsync(courseId, offeringId,
                sessionId, request.VirtualLink, cancellationToken);
            return result is null ? NotFound() : Ok(CourseOfferingResponse.From(result));
        }
        catch (ArgumentException exception) { return BadRequestProblem(exception.Message); }
    }

    private ObjectResult BadRequestProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid course offering", detail: detail);

    private ObjectResult ConflictProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status409Conflict, title: "Course offering conflict", detail: detail);
}
