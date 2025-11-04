using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesiCorner.MessageBus.Redis;

/// <summary>
/// Centralized Redis key management for all DesiCorner services
/// </summary>
public static class RedisKeys
{
    // Products
    public static string Product(Guid id) => $"product:{id}";
    public static string ProductList(string filter) => $"products:list:{filter}";
    public static string Category(Guid id) => $"category:{id}";
    public static string CategoryList() => $"categories:list";

    // Cart
    public static string UserCart(Guid userId) => $"cart:user:{userId}";
    public static string GuestCart(string sessionId) => $"cart:guest:{sessionId}";
    public static string CartTotal(Guid cartId) => $"cart:total:{cartId}";

    // Coupons
    public static string Coupon(string code) => $"coupon:{code}";
    public static string CouponUsage(string code, Guid userId) => $"coupon:usage:{code}:{userId}";

    // Orders
    public static string Order(Guid id) => $"order:{id}";
    public static string UserOrders(Guid userId) => $"orders:user:{userId}";
    public static string OrderStatus(Guid orderId) => $"order:status:{orderId}";

    // Payment
    public static string PaymentIntent(string paymentIntentId) => $"payment:intent:{paymentIntentId}";
    public static string PaymentSession(Guid orderId) => $"payment:session:{orderId}";
}
