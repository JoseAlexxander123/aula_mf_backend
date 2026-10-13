using Asp.Versioning;
using AulaVirtual.Modules.Authorization.Application;
using AulaVirtual.Modules.Courses.Api.Contracts;
using AulaVirtual.Modules.Courses.Application.Courses;
using AulaVirtual.SharedKernel.Application.Storage;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AulaVirtual.Modules.Courses.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/courses")]
[Authorize(Policy = Permissions.CoursesManage)]
public sealed class CoursesController(CourseUseCases useCases, IFileStorage? storage = null) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok((await useCases.ListAdminCoursesAsync(cancellationToken)).Select(details => CourseAdminResponse.From(details, storage)));

    [HttpGet("{courseId:guid}")]
    public async Task<IActionResult> Get(Guid courseId, CancellationToken cancellationToken)
    {
        var result = await useCases.GetAdminCourseByIdAsync(courseId, cancellationToken);
        return result is null ? NotFound() : Ok(CourseAdminResponse.From(result, storage));
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveCourseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.CreateCourseAsync(request.ToCommand(), cancellationToken);
            return CreatedAtAction(nameof(Get), new { version = "1", courseId = result.Id }, CourseAdminResponse.From(result, storage));
        }
        catch (ValidationException exception) { return Invalid(exception.Message); }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
    }

    [HttpPut("{courseId:guid}")]
    public async Task<IActionResult> Update(Guid courseId, SaveCourseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.UpdateCourseAsync(courseId, request.ToCommand(), cancellationToken);
            return result is null ? NotFound() : Ok(CourseAdminResponse.From(result, storage));
        }
        catch (ValidationException exception) { return Invalid(exception.Message); }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
        catch (InvalidOperationException exception) { return Conflict(exception.Message); }
    }

    [HttpPut("{courseId:guid}/publication")]
    public async Task<IActionResult> SetPublication(Guid courseId, SetCoursePublicationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.SetPublicationAsync(courseId, request.IsPublished, cancellationToken);
            return result is null ? NotFound() : Ok(CourseAdminResponse.From(result, storage));
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
        catch (InvalidOperationException exception) { return Conflict(exception.Message); }
    }

    [HttpPut("{courseId:guid}/cover")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ReplaceCover(Guid courseId, IFormFile file,
        [FromServices] CourseCoverUseCases? coverUseCases = null,
        CancellationToken cancellationToken = default)
    {
        if (coverUseCases is null)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Course cover storage is unavailable.");
        if (file is null) return Invalid("Cover file is required.");
        try
        {
            await using var content = file.OpenReadStream();
            var result = await coverUseCases.ReplaceCourseCoverAsync(courseId, content, file.ContentType,
                file.Length, file.FileName, cancellationToken);
            return result is null ? NotFound() : Ok(CourseAdminResponse.From(result, storage));
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
    }

    private ObjectResult Invalid(string detail) => Problem(statusCode: StatusCodes.Status400BadRequest,
        title: "Invalid course", detail: detail);

    private ObjectResult Conflict(string detail) => Problem(statusCode: StatusCodes.Status409Conflict,
        title: "Course conflict", detail: detail);
}
