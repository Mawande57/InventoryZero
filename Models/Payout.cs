using System;
using System.Collections.Generic;

namespace InventoryZeroAPI.Models;

public partial class Payout
{
    public int Id { get; set; }

    public int ShopId { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public string? StripeTransferId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ProcessedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Shop Shop { get; set; } = null!;
}
