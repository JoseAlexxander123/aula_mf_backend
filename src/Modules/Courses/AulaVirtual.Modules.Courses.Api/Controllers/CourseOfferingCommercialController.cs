using Asp.Versioning;
using AulaVirtual.Modules.Authorization.Application;
using AulaVirtual.Modules.Courses.Api.Contracts;
using AulaVirtual.Modules.Courses.Application.Offerings;
using AulaVirtual.Modules.Courses.Domain.Offerings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AulaVirtual.Modules.Courses.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/courses/{courseId:guid}/offerings/{offeringId:guid}")]
[Authorize(Policy = Permissions.CourseOfferingsManage)]
public sealed class CourseOfferingCommercialController(
    CourseOfferingCommercialUseCases useCases) : ControllerBase
{
    [HttpPut("instructors")]
    public async Task<IActionResult> AssignInstructors(Guid courseId, Guid offeringId,
        AssignInstructorsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.AssignInstructorsAsync(courseId, offeringId,
                request.InstructorIds, cancellationToken);
            return result is null ? NotFound() : Ok(CourseOfferingResponse.From(result));
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
        catch (InvalidOperationException exception) { return ConflictProblem(exception.Message); }
    }

    [HttpPut("currency")]
    public async Task<IActionResult> UpdateCurrency(Guid courseId, Guid offeringId,
        UpdateCurrencyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.UpdateCurrencyCodeAsync(courseId, offeringId,
                request.CurrencyCode, cancellationToken);
            return result is null ? NotFound() : Ok(CourseOfferingResponse.From(result));
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
        catch (InvalidOperationException exception) { return ConflictProblem(exception.Message); }
    }

    [HttpPut("specific-promotion")]
    public async Task<IActionResult> SetSpecificPromotion(Guid courseId, Guid offeringId,
        SetSpecificPromotionRequest request, CancellationToken cancellationToken)
    {
        var type = request.Type switch
        {
            "Percentage" => SpecificPromotionType.Percentage,
            "FixedAmount" => SpecificPromotionType.FixedAmount,
            _ => (SpecificPromotionType?)null
        };
        if (type is null) return Invalid("Promotion type must be Percentage or FixedAmount.");
        try
        {
            var result = await useCases.SetSpecificPromotionAsync(courseId, offeringId,
                type.Value, request.Value, request.StartsAt, request.EndsAt, cancellationToken);
            return result is null ? NotFound() : Ok(CourseOfferingResponse.From(result));
        }
        catch (ArgumentException exception) { return Invalid(exception.Message); }
        catch (InvalidOperationException exception) { return ConflictProblem(exception.Message); }
    }

    [HttpDelete("specific-promotion")]
    public async Task<IActionResult> RemoveSpecificPromotion(Guid courseId, Guid offeringId,
        CancellationToken cancellationToken)
    {
        var result = await useCases.RemoveSpecificPromotionAsync(courseId, offeringId, cancellationToken);
        return result is null ? NotFound() : Ok(CourseOfferingResponse.From(result));
    }

    [HttpGet("specific-price-preview")]
    public async Task<IActionResult> GetSpecificPricePreview(Guid courseId, Guid offeringId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCases.GetSpecificPricePreviewAsync(courseId, offeringId, cancellationToken);
            return result is null ? NotFound() : Ok(PricePreviewResponse.From(result));
        }
        catch (InvalidOperationException exception) { return ConflictProblem(exception.Message); }
    }

    private ObjectResult Invalid(string detail) => Problem(statusCode: 400,
        title: "Invalid course offering data", detail: detail);

    private ObjectResult ConflictProblem(string detail) => Problem(statusCode: 409,
        title: "Course offering conflict", detail: detail);
}
