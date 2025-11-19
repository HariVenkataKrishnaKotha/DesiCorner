using DesiCorner.Contracts.Coupons;
using DesiCorner.Services.CartAPI.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace DesiCorner.Services.CartAPI.Services;

public class CartService : ICartService
{
    private readonly IDatabase _redisDb;
    private readonly IProductService _productService;
    private readonly ICouponService _couponService;
    private readonly ILogger<CartService> _logger;

    private const decimal TAX_RATE = 0.08m; // 8% tax
    private const decimal DELIVERY_FEE = 5.00m;
    private const decimal FREE_DELIVERY_THRESHOLD = 50.00m;

    public CartService(
        IConnectionMultiplexer redis,
        IProductService productService,
        ICouponService couponService,
        ILogger<CartService> logger)
    {
        _redisDb = redis.GetDatabase();
        _productService = productService;
        _couponService = couponService;
        _logger = logger;
    }

    public async Task<Cart?> GetCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default)
    {
        try
        {
            var key = GetCartKey(userId, sessionId);
            var cartJson = await _redisDb.StringGetAsync(key);

            if (cartJson.IsNullOrEmpty)
            {
                return null;
            }

            var cart = JsonConvert.DeserializeObject<Cart>(cartJson!);

            if (cart != null)
            {
                await RecalculateCartAsync(cart, ct);
            }

            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart");
            throw;
        }
    }

    public async Task<Cart> AddToCartAsync(Guid? userId, string? sessionId, Guid productId, int quantity, CancellationToken ct = default)
    {
        try
        {
            // Fetch product details
            var product = await _productService.GetProductAsync(productId, ct);

            if (product == null)
            {
                throw new ArgumentException($"Product {productId} not found");
            }

            if (!product.IsAvailable)
            {
                throw new ArgumentException($"Product {product.Name} is currently unavailable");
            }

            // Get or create cart
            var cart = await GetCartAsync(userId, sessionId, ct) ?? new Cart
            {
                UserId = userId,
                SessionId = sessionId
            };

            // Check if item exists
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                _logger.LogInformation("Updated quantity for product {ProductId} in cart. New quantity: {Quantity}",
                    productId, existingItem.Quantity);
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductImage = product.ImageUrl,
                    Price = product.Price,
                    Quantity = quantity
                });
                _logger.LogInformation("Added product {ProductId} to cart with quantity {Quantity}",
                    productId, quantity);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await RecalculateCartAsync(cart, ct);
            await SaveCartAsync(cart);

            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            throw;
        }
    }

    public async Task<Cart> UpdateCartItemAsync(Guid cartItemId, int quantity, CancellationToken ct = default)
    {
        try
        {
            var cart = await FindCartByItemIdAsync(cartItemId);

            if (cart == null)
            {
                throw new ArgumentException($"Cart item {cartItemId} not found");
            }

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (item == null)
            {
                throw new ArgumentException($"Cart item {cartItemId} not found");
            }

            item.Quantity = quantity;
            cart.UpdatedAt = DateTime.UtcNow;

            await RecalculateCartAsync(cart, ct);
            await SaveCartAsync(cart);

            _logger.LogInformation("Updated cart item {ItemId} quantity to {Quantity}", cartItemId, quantity);

            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item");
            throw;
        }
    }

    public async Task<bool> RemoveFromCartAsync(Guid cartItemId, CancellationToken ct = default)
    {
        try
        {
            var cart = await FindCartByItemIdAsync(cartItemId);

            if (cart == null)
            {
                return false;
            }

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (item == null)
            {
                return false;
            }

            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;

            if (cart.Items.Count == 0)
            {
                await ClearCartAsync(cart.UserId, cart.SessionId, ct);
            }
            else
            {
                await RecalculateCartAsync(cart, ct);
                await SaveCartAsync(cart);
            }

            _logger.LogInformation("Removed cart item {ItemId}", cartItemId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item");
            throw;
        }
    }

    public async Task<bool> ClearCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default)
    {
        try
        {
            var key = GetCartKey(userId, sessionId);
            await _redisDb.KeyDeleteAsync(key);

            _logger.LogInformation("Cleared cart for key {Key}", key);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            throw;
        }
    }

    public async Task<Cart> ApplyCouponAsync(Guid cartId, string couponCode, CancellationToken ct = default)
    {
        try
        {
            var cart = await FindCartByIdAsync(cartId);

            if (cart == null)
            {
                throw new ArgumentException($"Cart {cartId} not found");
            }

            // Validate coupon
            var validationRequest = new ValidateCouponRequestDto
            {
                Code = couponCode,
                CartTotal = cart.SubTotal,
                UserId = cart.UserId
            };

            var validationResult = await _couponService.ValidateCouponAsync(validationRequest, ct);

            if (!validationResult.IsValid)
            {
                throw new ArgumentException(validationResult.Message);
            }

            cart.CouponCode = couponCode;
            cart.DiscountAmount = validationResult.DiscountAmount;
            cart.UpdatedAt = DateTime.UtcNow;

            await RecalculateCartAsync(cart, ct);
            await SaveCartAsync(cart);

            _logger.LogInformation("Applied coupon {CouponCode} to cart {CartId}. Discount: ${Discount}",
                couponCode, cartId, validationResult.DiscountAmount);

            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying coupon");
            throw;
        }
    }

    public async Task<Cart> RemoveCouponAsync(Guid cartId, CancellationToken ct = default)
    {
        try
        {
            var cart = await FindCartByIdAsync(cartId);

            if (cart == null)
            {
                throw new ArgumentException($"Cart {cartId} not found");
            }

            cart.CouponCode = null;
            cart.DiscountAmount = 0;
            cart.UpdatedAt = DateTime.UtcNow;

            await RecalculateCartAsync(cart, ct);
            await SaveCartAsync(cart);

            _logger.LogInformation("Removed coupon from cart {CartId}", cartId);

            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing coupon");
            throw;
        }
    }

    private async Task RecalculateCartAsync(Cart cart, CancellationToken ct = default)
    {
        // Calculate subtotal
        cart.SubTotal = cart.Items.Sum(i => i.Total);

        // Revalidate coupon if exists
        if (!string.IsNullOrEmpty(cart.CouponCode))
        {
            var validationRequest = new ValidateCouponRequestDto
            {
                Code = cart.CouponCode,
                CartTotal = cart.SubTotal,
                UserId = cart.UserId
            };

            var validationResult = await _couponService.ValidateCouponAsync(validationRequest, ct);

            if (validationResult.IsValid)
            {
                cart.DiscountAmount = validationResult.DiscountAmount;
            }
            else
            {
                // Coupon is no longer valid, remove it
                cart.CouponCode = null;
                cart.DiscountAmount = 0;
                _logger.LogWarning("Coupon {CouponCode} is no longer valid and was removed", cart.CouponCode);
            }
        }

        // Calculate tax (on subtotal after discount)
        var taxableAmount = Math.Max(0, cart.SubTotal - cart.DiscountAmount);
        cart.TaxAmount = taxableAmount * TAX_RATE;

        // Calculate delivery fee
        cart.DeliveryFee = cart.SubTotal >= FREE_DELIVERY_THRESHOLD ? 0 : DELIVERY_FEE;

        // Calculate total
        cart.Total = cart.SubTotal - cart.DiscountAmount + cart.TaxAmount + cart.DeliveryFee;
    }

    private async Task SaveCartAsync(Cart cart)
    {
        var key = GetCartKey(cart.UserId, cart.SessionId);
        var cartJson = JsonConvert.SerializeObject(cart);
        await _redisDb.StringSetAsync(key, cartJson, TimeSpan.FromDays(30));
    }

    private string GetCartKey(Guid? userId, string? sessionId)
    {
        if (userId.HasValue)
        {
            return $"cart:user:{userId.Value}";
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            return $"cart:session:{sessionId}";
        }

        throw new ArgumentException("Either userId or sessionId must be provided");
    }

    private async Task<Cart?> FindCartByIdAsync(Guid cartId)
    {
        var server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: "cart:*");

        foreach (var key in keys)
        {
            var cartJson = await _redisDb.StringGetAsync(key);
            if (!cartJson.IsNullOrEmpty)
            {
                var cart = JsonConvert.DeserializeObject<Cart>(cartJson!);
                if (cart?.Id == cartId)
                {
                    return cart;
                }
            }
        }

        return null;
    }

    private async Task<Cart?> FindCartByItemIdAsync(Guid cartItemId)
    {
        var server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: "cart:*");

        foreach (var key in keys)
        {
            var cartJson = await _redisDb.StringGetAsync(key);
            if (!cartJson.IsNullOrEmpty)
            {
                var cart = JsonConvert.DeserializeObject<Cart>(cartJson!);
                if (cart?.Items.Any(i => i.Id == cartItemId) == true)
                {
                    return cart;
                }
            }
        }

        return null;
    }
}