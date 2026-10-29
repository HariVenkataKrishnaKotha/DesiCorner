using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DesiCorner.AuthServer.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DesiCorner.AuthServer.Tests;

/// <summary>
/// GET /api/account/user-lookup tells whether an email or phone number has an account, so it
/// is for OrderAPI only: it requires a client-credentials token carrying the dedicated
/// desicorner.internal.user-lookup scope. Anonymous callers and every user token, an admin's
/// included, are refused, even though user tokens have the same audience.
/// </summary>
public class UserLookupAccessTests
{
    private const string LookupUrl = "/api/account/user-lookup?email=known%40example.test";

    private static readonly string[] SpaScopes =
    {
        "openid", "profile", "email", "phone", "offline_access",
        "desicorner.products.read", "desicorner.cart", "desicorner.orders.read", "desicorner.orders.write", "desicorner.payment"
    };

    private static async Task<Guid> SeedUser(AuthServerFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "known@example.test", Email = "known@example.test", CreatedAt = DateTime.UtcNow };
        Assert.True((await users.CreateAsync(user)).Succeeded);
        return user.Id;
    }

    private static Task<HttpResponseMessage> RequestToken(AuthServerFactory factory, string clientId, string? secret, string scope) =>
        factory.CreateClient().PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = secret ?? "",
            ["scope"] = scope
        }));

    private static async Task<string> ServiceToken(AuthServerFactory factory, string secret)
    {
        var response = await RequestToken(factory, ServiceSecrets.OrderServiceClientId, secret, ServiceSecrets.UserLookupScope);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    private static async Task<HttpResponseMessage> Lookup(AuthServerFactory factory, string? token)
    {
        var client = factory.CreateClient();
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await client.GetAsync(LookupUrl);
    }

    private static async Task<string?> ErrorCode(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("error", out var e) ? e.GetString() : null;
    }

    [Fact]
    public async Task An_anonymous_caller_gets_401()
    {
        using var factory = new AuthServerFactory(settings: ServiceSecrets.Both(out _));
        await SeedUser(factory);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Lookup(factory, null)).StatusCode);
    }

    [Fact]
    public async Task A_customer_token_is_refused()
    {
        using var factory = new AuthServerFactory(settings: ServiceSecrets.Both(out _));
        await SeedUser(factory);

        var response = await Lookup(factory, factory.CreateUserToken(Guid.NewGuid(), new[] { "Customer" }, SpaScopes));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_admin_token_with_every_user_scope_is_refused()
    {
        using var factory = new AuthServerFactory(settings: ServiceSecrets.Both(out _));
        await SeedUser(factory);

        var response = await Lookup(factory, factory.CreateUserToken(Guid.NewGuid(), new[] { "Admin" },
            SpaScopes.Append("desicorner.products.write").Append("desicorner.admin").ToArray()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_order_service_gets_a_client_credentials_token_and_can_look_users_up()
    {
        using var factory = new AuthServerFactory(settings: ServiceSecrets.Both(out var secret));
        var userId = await SeedUser(factory);

        var response = await Lookup(factory, await ServiceToken(factory, secret));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(userId, json.RootElement.GetProperty("result").GetProperty("userId").GetGuid());
    }

    [Fact]
    public async Task A_wrong_client_secret_gets_no_token()
    {
        using var factory = new AuthServerFactory(settings: ServiceSecrets.Both(out _));

        var response = await RequestToken(factory, ServiceSecrets.OrderServiceClientId, ServiceSecrets.New(), ServiceSecrets.UserLookupScope);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal("invalid_client", await ErrorCode(response));
    }

    [Fact]
    public async Task The_SPA_client_cannot_use_the_client_credentials_grant()
    {
        using var factory = new AuthServerFactory(settings: ServiceSecrets.Both(out _));

        var response = await RequestToken(factory, "desicorner-angular", null, ServiceSecrets.UserLookupScope);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task The_order_service_cannot_request_user_scopes()
    {
        using var factory = new AuthServerFactory(settings: ServiceSecrets.Both(out var secret));

        var response = await RequestToken(factory, ServiceSecrets.OrderServiceClientId, secret, "desicorner.admin");

        Assert.False(response.IsSuccessStatusCode);
    }
}
