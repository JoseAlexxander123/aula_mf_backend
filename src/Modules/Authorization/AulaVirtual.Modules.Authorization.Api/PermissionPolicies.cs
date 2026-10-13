using AulaVirtual.Modules.Authorization.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AulaVirtual.Modules.Authorization.Api;

public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!Permissions.All.Contains(policyName)) return fallback.GetPolicyAsync(policyName);
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(policyName)).Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => fallback.GetFallbackPolicyAsync();
}

public sealed class PermissionAuthorizationHandler(IPermissionChecker checker)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(context.User.FindFirst("user_id")?.Value, out var userId) ||
            userId == Guid.Empty) return;
        if (await checker.HasPermissionAsync(userId, requirement.Code, CancellationToken.None))
            context.Succeed(requirement);
    }
}
