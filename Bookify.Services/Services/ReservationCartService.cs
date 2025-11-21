using Bookify.Core.Models;
using Microsoft.AspNetCore.Http;

namespace Bookify.Services.Services
{
    public class ReservationCartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartSessionKey = "ReservationCart";

        public ReservationCartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void AddToCart(ReservationCart cart)
        {
            _httpContextAccessor.HttpContext?.Session.SetString(CartSessionKey,
                System.Text.Json.JsonSerializer.Serialize(cart));
        }

        public ReservationCart? GetCart()
        {
            var cartJson = _httpContextAccessor.HttpContext?.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
                return null;

            return System.Text.Json.JsonSerializer.Deserialize<ReservationCart>(cartJson);
        }

        public void ClearCart()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(CartSessionKey);
        }

        public bool HasItems()
        {
            return GetCart() != null;
        }
    }
}