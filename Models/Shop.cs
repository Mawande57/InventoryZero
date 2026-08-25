using System;
using System.Collections.Generic;

namespace InventoryZeroAPI.Models;

public partial class Shop
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ShopName { get; set; } = null!;

    public string? ShopDescription { get; set; }

    public string? LogoUrl { get; set; }

    public string? CoverImageUrl { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? PostalCode { get; set; }

    public string Country { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? PhoneNumber { get; set; }

    public string? BusinessRegistrationNumber { get; set; }

    public string? TaxNumber { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? VerificationDate { get; set; }

    public string? VerificationNotes { get; set; }

    public decimal CommissionRate { get; set; }

    public int PayoutDelayDays { get; set; }
    public int TotalSales { get; set; } = 0;
    public decimal TotalRevenue { get; set; } = 0;

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Payout> Payouts { get; set; } = new List<Payout>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual User User { get; set; } = null!;
}
