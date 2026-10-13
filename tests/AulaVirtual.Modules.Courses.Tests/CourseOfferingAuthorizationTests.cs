using AulaVirtual.Modules.Authorization.Application;
using AulaVirtual.Modules.Courses.Api.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class CourseOfferingAuthorizationTests
{
    [Fact]
    public void AdministrativeControllers_RequireTheirPermission()
    {
        foreach (var (controller, permission) in new[]
        {
            (typeof(CoursesController), Permissions.CoursesManage),
            (typeof(CourseCategoriesController), Permissions.CoursesManage),
            (typeof(CourseOfferingsController), Permissions.CourseOfferingsManage),
            (typeof(CourseOfferingCommercialController), Permissions.CourseOfferingsManage),
            (typeof(InstructorsController), Permissions.InstructorsManage)
        })
        {
            var attribute = Assert.Single(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());
            Assert.Equal(permission, attribute.Policy);
        }
    }
}
