using DesiCorner.Contracts.Common;

namespace DesiCorner.Services.OrderAPI.Services;

public class UserService : IUserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UserService> _logger;

    public UserService(IHttpClientFactory httpClientFactory, ILogger<UserService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Guid?> GetUserIdByEmailOrPhoneAsync(string? email, string? phone, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthAPI");

            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(email))
                queryParams.Add($"email={Uri.EscapeDataString(email)}");
            if (!string.IsNullOrWhiteSpace(phone))
                queryParams.Add($"phone={Uri.EscapeDataString(phone)}");

            var url = $"/api/account/user-lookup?{string.Join("&", queryParams)}";

            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to lookup user. Status: {Status}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ResponseDto>(ct);

            if (result?.IsSuccess == true && result.Result != null)
            {
                var userInfo = System.Text.Json.JsonSerializer.Deserialize<UserLookupResult>(
                    result.Result.ToString() ?? "{}");

                if (userInfo?.Exists == true && !string.IsNullOrWhiteSpace(userInfo.UserId))
                {
                    return Guid.Parse(userInfo.UserId);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up user by email/phone");
            return null;
        }
    }

    private class UserLookupResult
    {
        public string? UserId { get; set; }
        public bool Exists { get; set; }
    }
}