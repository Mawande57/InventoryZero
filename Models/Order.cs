using System;
using System.Collections.Generic;

namespace InventoryZeroAPI.Models;

public partial class Order
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = null!;

    public int BuyerId { get; set; }

    

    public int ShopId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }

    public decimal ShippingCost { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PlatformFee { get; set; }

    public decimal SellerPayout { get; set; }

    public string? PaymentIntentId { get; set; }

    public string? PaymentMethod { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string ShippingAddressLine1 { get; set; } = null!;

    public string? ShippingAddressLine2 { get; set; }

    public string ShippingCity { get; set; } = null!;

    public string ShippingProvince { get; set; } = null!;

    public string ShippingPostalCode { get; set; } = null!;

    public string ShippingCountry { get; set; } = null!;

    public string ShippingPhoneNumber { get; set; } = null!;

    public string? TrackingNumber { get; set; }

    public string? TrackingCarrier { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public string? BuyerNotes { get; set; }

    public string? SellerNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Buyer { get; set; } = null!;

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payout> Payouts { get; set; } = new List<Payout>();

   

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Shop Shop { get; set; } = null!;
}
