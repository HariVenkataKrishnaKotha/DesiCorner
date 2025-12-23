using DesiCorner.Contracts.Coupons;

namespace DesiCorner.Services.CartAPI.Services;

public interface ICouponService
{
    // Existing method
    Task<ValidateCouponResponseDto> ValidateCouponAsync(ValidateCouponRequestDto request, CancellationToken ct = default);

    // Admin methods
    Task<List<CouponDto>> GetAllCouponsAsync(CouponFilterDto filter, CancellationToken ct = default);
    Task<CouponDto?> GetCouponByIdAsync(Guid id, CancellationToken ct = default);
    Task<CouponDto?> GetCouponByCodeAsync(string code, CancellationToken ct = default);
    Task<CouponDto> CreateCouponAsync(CreateCouponDto dto, string createdBy, CancellationToken ct = default);
    Task<CouponDto?> UpdateCouponAsync(UpdateCouponDto dto, CancellationToken ct = default);
    Task<bool> DeleteCouponAsync(Guid id, CancellationToken ct = default);
    Task<bool> ToggleCouponStatusAsync(Guid id, CancellationToken ct = default);
    Task IncrementUsageAsync(string code, CancellationToken ct = default);
    Task<CouponStatsDto> GetCouponStatsAsync(CancellationToken ct = default);
}