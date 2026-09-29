using System.ComponentModel.DataAnnotations;
using ggChick.Models;
using ggChick.ViewModels.Cart;

namespace ggChick.ViewModels.Orders
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping address is required.")]
        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
        [Display(Name = "Delivery Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Order Notes (optional)")]
        public string? Notes { get; set; }

        public CartViewModel Cart { get; set; } = new CartViewModel();
    }

    public class OrderListViewModel
    {
        public List<Order> Orders { get; set; } = new List<Order>();
    }
}
