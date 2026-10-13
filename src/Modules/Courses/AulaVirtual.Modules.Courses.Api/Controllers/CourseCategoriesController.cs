using Asp.Versioning;
using AulaVirtual.Modules.Authorization.Application;
using AulaVirtual.Modules.Courses.Api.Contracts;
using AulaVirtual.Modules.Courses.Application.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AulaVirtual.Modules.Courses.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/course-categories")]
[Authorize(Policy = Permissions.CoursesManage)]
public sealed class CourseCategoriesController(CourseUseCases useCases) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok((await useCases.ListCourseCategoriesAsync(cancellationToken)).Select(CourseCategoryResponse.From));

    [HttpGet("{categoryId:guid}")]
    public async Task<IActionResult> Get(Guid categoryId, CancellationToken cancellationToken)
    {
        try
        {
            var category = await useCases.GetCourseCategoryAsync(categoryId, cancellationToken);
            return category is null ? Missing() : Ok(CourseCategoryResponse.From(category));
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveCourseCategoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var category = await useCases.CreateCourseCategoryAsync(request.Name, cancellationToken);
            return CreatedAtAction(nameof(Get), new { version = "1", categoryId = category.Id },
                CourseCategoryResponse.From(category));
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
    }

    [HttpPut("{categoryId:guid}")]
    public async Task<IActionResult> Update(Guid categoryId, SaveCourseCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await useCases.UpdateCourseCategoryAsync(categoryId, request.Name, cancellationToken);
            return category is null ? Missing() : Ok(CourseCategoryResponse.From(category));
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
    }

    [HttpDelete("{categoryId:guid}")]
    public async Task<IActionResult> Delete(Guid categoryId, CancellationToken cancellationToken)
    {
        try
        {
            return await useCases.DeleteCourseCategoryAsync(categoryId, cancellationToken) switch
            {
                CourseCategoryDeletion.Deleted => NoContent(),
                CourseCategoryDeletion.NotFound => Missing(),
                CourseCategoryDeletion.InUse => ConflictProblem(),
                _ => throw new InvalidOperationException("Unknown category deletion result.")
            };
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
    }

    private ObjectResult Invalid(string detail) => Problem(statusCode: StatusCodes.Status400BadRequest,
        title: "Invalid course category", detail: detail);

    private ObjectResult Missing() => Problem(statusCode: StatusCodes.Status404NotFound,
        title: "Course category not found");

    private ObjectResult ConflictProblem() => Problem(statusCode: StatusCodes.Status409Conflict,
        title: "Course category in use", detail: "The category is assigned to a course.");
}
