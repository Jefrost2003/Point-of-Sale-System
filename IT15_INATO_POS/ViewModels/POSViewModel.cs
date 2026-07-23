using IT15_INATO_POS.Models;
using System.Collections.Generic;

namespace IT15_INATO_POS.ViewModels
{
#pragma warning disable S101
    public class POSViewModel
#pragma warning restore S101
    {
        public List<Product> Products { get; set; } = new();
        public List<CartItemViewModel> CartItems { get; set; } = new();
        public decimal CartTotal { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public decimal AmountPaid { get; set; }
        public decimal ChangeAmount { get; set; }
    }

    public class CartItemViewModel
    {
        public int VariationID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public int AvailableStock { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new();
#pragma warning disable S6964
        public decimal TotalAmount { get; set; }
#pragma warning restore S6964
#pragma warning disable S6964
        public decimal AmountPaid { get; set; }
#pragma warning restore S6964
#pragma warning disable S6964
        public decimal ChangeAmount { get; set; }
#pragma warning restore S6964
        public string PaymentMethod { get; set; } = "Cash";
        public string? PayMongoReference { get; set; }
        public string? Notes { get; set; }
    }
}