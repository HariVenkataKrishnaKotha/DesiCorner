using Microsoft.AspNetCore.Authorization;

namespace DesiCorner.Gateway.Policies;

public sealed class ScopeRequirement : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> Required { get; }
    public ScopeRequirement(params string[] scopes) => Required = scopes;
}