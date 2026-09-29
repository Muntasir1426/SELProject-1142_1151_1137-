namespace ggChick.ViewModels.Cart
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public int StockQuantity { get; set; }

        public decimal Subtotal => Quantity * UnitPrice;
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal Subtotal => Items.Sum(i => i.Subtotal);
        public decimal EstimatedTax => Math.Round(Subtotal * 0.05m, 2); // 5% tax
        public decimal ShippingFee => Subtotal > 1000m || Subtotal == 0m ? 0m : 100m; // Free shipping over $100
        public decimal GrandTotal => Subtotal + EstimatedTax + ShippingFee;
        public int TotalItemCount => Items.Sum(i => i.Quantity);
    }
}
