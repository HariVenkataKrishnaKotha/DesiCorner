using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DesiCorner.Contracts.Cart;
using DesiCorner.Contracts.Coupons;
using DesiCorner.Services.CartAPI.Models;

namespace DesiCorner.Services.CartAPI.Services;

public class CartService : ICartService
{
    private readonly ICartStore _cartStore;
    private readonly IProductService _productService;
    private readonly ICouponService _couponService;
    private readonly ILogger<CartService> _logger;

    private const decimal TAX_RATE = 0.06m; // 6% tax
    private const decimal DELIVERY_FEE = 5.00m;
    private const decimal FREE_DELIVERY_THRESHOLD = 50.00m;

    // The per-line quantity limit the API already declares ([Range] on UpdateCartItemDto.Quantity).
    // Read from the contract so a merge can't put a line above what an update would accept.
    private static readonly int MaxQuantityPerItem = (int)typeof(UpdateCartItemDto)
        .GetProperty(nameof(UpdateCartItemDto.Quantity))!
        .GetCustomAttribute<RangeAttribute>()!.Maximum;

    public CartService(
        ICartStore cartStore,
        IProductService productService,
        ICouponService couponService,
        ILogger<CartService> logger)
    {
        _cartStore = cartStore;
        _productService = productService;
        _couponService = couponService;
        _logger = logger;
    }

    public async Task<Cart?> GetCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default)
    {
        try
        {
            var key = GetCartKey(userId, sessionId);
            var cart = await _cartStore.GetAsync(key);

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

    public async Task<Cart?> UpdateCartItemAsync(Guid? userId, string? sessionId, Guid cartItemId, int quantity, CancellationToken ct = default)
    {
        try
        {
            var cart = await GetCartAsync(userId, sessionId, ct);
            var item = cart?.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (cart == null || item == null)
            {
                return null;
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

    public async Task<bool> RemoveFromCartAsync(Guid? userId, string? sessionId, Guid cartItemId, CancellationToken ct = default)
    {
        try
        {
            var cart = await GetCartAsync(userId, sessionId, ct);
            var item = cart?.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (cart == null || item == null)
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
            await _cartStore.DeleteAsync(key);

            _logger.LogInformation("Cleared cart for key {Key}", key);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            throw;
        }
    }

    public async Task<Cart?> ApplyCouponAsync(Guid? userId, string? sessionId, Guid cartId, string couponCode, CancellationToken ct = default)
    {
        try
        {
            var cart = await GetCartAsync(userId, sessionId, ct);

            if (cart == null || cart.Id != cartId)
            {
                return null;
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

    public async Task<Cart?> RemoveCouponAsync(Guid? userId, string? sessionId, Guid cartId, CancellationToken ct = default)
    {
        try
        {
            var cart = await GetCartAsync(userId, sessionId, ct);

            if (cart == null || cart.Id != cartId)
            {
                return null;
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

    public async Task<CartMergeResult> MergeGuestCartAsync(Guid userId, string sessionId, CancellationToken ct = default)
    {
        try
        {
            // Read exactly this session's cart by its key; never search other carts.
            var guestKey = GetCartKey(null, sessionId);
            var guestCart = await _cartStore.GetAsync(guestKey);

            if (guestCart == null || guestCart.Items.Count == 0)
            {
                if (guestCart != null)
                {
                    await _cartStore.DeleteAsync(guestKey);
                }

                return new CartMergeResult(await GetCartAsync(userId, null, ct), Array.Empty<DroppedCartItem>(), null);
            }

            // Look every guest product up first, so an outage aborts before anything changes.
            var lookups = new List<(CartItem Item, ProductLookup Lookup)>();
            foreach (var item in guestCart.Items)
            {
                var lookup = await _productService.LookupProductAsync(item.ProductId, ct);
                if (lookup.Status == ProductLookupStatus.Failed)
                {
                    throw new ProductCatalogUnavailableException(
                        $"Could not look up product {item.ProductId}; the guest cart was not merged");
                }
                lookups.Add((item, lookup));
            }

            var userCart = await _cartStore.GetAsync(GetCartKey(userId, null)) ?? new Cart { UserId = userId };
            var dropped = new List<DroppedCartItem>();

            foreach (var (item, lookup) in lookups)
            {
                var product = lookup.Product;
                if (product == null || !product.IsAvailable)
                {
                    dropped.Add(new DroppedCartItem(item.ProductId, item.ProductName, item.Quantity,
                        product == null ? DroppedCartItem.NotFound : DroppedCartItem.Unavailable));
                    continue;
                }

                // Same rules as AddToCartAsync: an existing line keeps its price and gains the
                // quantity; a new line takes the current ProductAPI price, never the stored guest price.
                var existing = userCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (existing != null)
                {
                    existing.Quantity = Math.Min(existing.Quantity + item.Quantity, MaxQuantityPerItem);
                }
                else
                {
                    userCart.Items.Add(new CartItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductImage = product.ImageUrl,
                        Price = product.Price,
                        Quantity = Math.Min(item.Quantity, MaxQuantityPerItem)
                    });
                }
            }

            // Coupon: the user's own coupon wins. Otherwise carry the guest's over only if it is
            // still valid for this user and the merged cart.
            string? droppedCoupon = null;
            if (string.IsNullOrEmpty(userCart.CouponCode) && !string.IsNullOrEmpty(guestCart.CouponCode))
            {
                var validation = await _couponService.ValidateCouponAsync(new ValidateCouponRequestDto
                {
                    Code = guestCart.CouponCode,
                    CartTotal = userCart.Items.Sum(i => i.Total),
                    UserId = userId
                }, ct);

                if (validation.IsValid)
                {
                    userCart.CouponCode = guestCart.CouponCode;
                    userCart.DiscountAmount = validation.DiscountAmount;
                }
                else
                {
                    droppedCoupon = guestCart.CouponCode;
                }
            }

            Cart? merged = null;
            if (userCart.Items.Count > 0)
            {
                userCart.UpdatedAt = DateTime.UtcNow;
                await RecalculateCartAsync(userCart, ct);
                await SaveCartAsync(userCart);
                merged = userCart;
            }

            await _cartStore.DeleteAsync(guestKey);

            _logger.LogInformation(
                "Merged guest cart into user {UserId}: {Moved} line(s) moved, {Dropped} dropped",
                userId, guestCart.Items.Count - dropped.Count, dropped.Count);

            return new CartMergeResult(merged, dropped, droppedCoupon);
        }
        catch (Exception ex) when (ex is not ProductCatalogUnavailableException)
        {
            _logger.LogError(ex, "Error merging guest cart");
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
        await _cartStore.SaveAsync(key, cart, TimeSpan.FromDays(30));
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
}