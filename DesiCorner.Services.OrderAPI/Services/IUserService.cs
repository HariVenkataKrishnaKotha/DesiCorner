namespace DesiCorner.Services.OrderAPI.Services;

public interface IUserService
{
    Task<Guid?> GetUserIdByEmailOrPhoneAsync(string? email, string? phone, CancellationToken ct = default);
}