using ggChick.ViewModels.Cart;

namespace ggChick.Services
{
    public interface ICartService
    {
        CartViewModel GetCart();
        void AddToCart(int productId, string name, decimal price, string? imageUrl, int stock, int quantity = 1);
        void UpdateQuantity(int productId, int quantity);
        void RemoveFromCart(int productId);
        void ClearCart();
        int GetCartCount();
    }
}
