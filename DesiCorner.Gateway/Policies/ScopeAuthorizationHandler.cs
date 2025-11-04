using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DesiCorner.Gateway.Policies;

public sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        var scopes = context.User.FindAll("scope").Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        if (requirement.Required.All(scopes.Contains))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}