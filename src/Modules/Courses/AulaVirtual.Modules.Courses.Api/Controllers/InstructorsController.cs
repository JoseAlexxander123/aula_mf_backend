using Asp.Versioning;
using AulaVirtual.Modules.Authorization.Application;
using AulaVirtual.Modules.Courses.Api.Contracts;
using AulaVirtual.Modules.Courses.Application.Instructors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AulaVirtual.Modules.Courses.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/instructors")]
[Authorize(Policy = Permissions.InstructorsManage)]
public sealed class InstructorsController(InstructorUseCases useCases) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var result = await useCases.CreateInstructorAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { version = "1", instructorId = result.Id },
            InstructorResponse.From(result));
    }

    [HttpGet("{instructorId:guid}")]
    public async Task<IActionResult> Get(Guid instructorId, CancellationToken cancellationToken)
    {
        var result = await useCases.GetInstructorAsync(instructorId, cancellationToken);
        return result is null ? NotFound() : Ok(InstructorResponse.From(result));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok((await useCases.ListInstructorsAsync(cancellationToken)).Select(InstructorResponse.From));

    [HttpPut("{instructorId:guid}/user-link")]
    public async Task<IActionResult> LinkUser(Guid instructorId, LinkInstructorUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.LinkUserAsync(instructorId, request.UserId, cancellationToken);
            return result.Status switch
            {
                LinkUserStatus.Linked => Ok(InstructorResponse.From(result.Instructor!)),
                LinkUserStatus.InstructorNotFound or LinkUserStatus.UserNotFound => NotFound(),
                LinkUserStatus.UserAlreadyLinked => Conflict(),
                _ => throw new InvalidOperationException("Unexpected link result.")
            };
        }
        catch (ArgumentException exception)
        {
            return Problem(statusCode: 400, title: "Invalid Instructor link", detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: 409, title: "Instructor link conflict", detail: exception.Message);
        }
    }
}
