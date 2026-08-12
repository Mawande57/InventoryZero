using System;
using System.Collections.Generic;

namespace InventoryZeroAPI.Models;

public partial class Review
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int ReviewerId { get; set; }

    public int ShopId { get; set; }

    public int Rating { get; set; }

    public string? Title { get; set; }

    public string? Comment { get; set; }

    public string? Pros { get; set; }

    public string? Cons { get; set; }

    public bool IsVerifiedPurchase { get; set; }

    public string? SellerResponse { get; set; }

    public DateTime? SellerResponseAt { get; set; }

    public int HelpfulCount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual User Reviewer { get; set; } = null!;

    public virtual Shop Shop { get; set; } = null!;
}
