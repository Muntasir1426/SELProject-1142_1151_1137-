using System.Text.Json;
using ggChick.ViewModels.Cart;

namespace ggChick.Services
{
    public class CartService : ICartService
    {
        private const string CartSessionKey = "GGCLIKS_CART_SESSION";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session 
            ?? throw new InvalidOperationException("Session is not available.");

        public CartViewModel GetCart()
        {
            var cartJson = Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new CartViewModel();
            }

            try
            {
                return JsonSerializer.Deserialize<CartViewModel>(cartJson) ?? new CartViewModel();
            }
            catch
            {
                return new CartViewModel();
            }
        }

        private void SaveCart(CartViewModel cart)
        {
            var json = JsonSerializer.Serialize(cart);
            Session.SetString(CartSessionKey, json);
        }

        public void AddToCart(int productId, string name, decimal price, string? imageUrl, int stock, int quantity = 1)
        {
            var cart = GetCart();
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem != null)
            {
                var newQty = existingItem.Quantity + quantity;
                existingItem.Quantity = Math.Min(newQty, stock);
            }
            else
            {
                cart.Items.Add(new CartItemViewModel
                {
                    ProductId = productId,
                    ProductName = name,
                    UnitPrice = price,
                    Quantity = Math.Min(quantity, stock),
                    ImageUrl = imageUrl,
                    StockQuantity = stock
                });
            }

            SaveCart(cart);
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Items.Remove(item);
                }
                else
                {
                    item.Quantity = Math.Min(quantity, item.StockQuantity);
                }
                SaveCart(cart);
            }
        }

        public void RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                cart.Items.Remove(item);
                SaveCart(cart);
            }
        }

        public void ClearCart()
        {
            Session.Remove(CartSessionKey);
        }

        public int GetCartCount()
        {
            return GetCart().TotalItemCount;
        }
    }
}
