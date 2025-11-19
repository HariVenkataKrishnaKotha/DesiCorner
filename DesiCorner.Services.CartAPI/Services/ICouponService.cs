using DesiCorner.Contracts.Coupons;

namespace DesiCorner.Services.CartAPI.Services;

public interface ICouponService
{
    Task<ValidateCouponResponseDto> ValidateCouponAsync(ValidateCouponRequestDto request, CancellationToken ct = default);
}