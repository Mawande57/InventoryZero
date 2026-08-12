using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryZeroAPI.Models;

public partial class Product
{
    public int Id { get; set; }

    public int ShopId { get; set; }

    public int? CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public string? Sku { get; set; }

    public string? Barcode { get; set; }

    public decimal OriginalPrice { get; set; }

    public decimal SalePrice { get; set; }

    public decimal DiscountPercentage { get; set; }

    public int Quantity { get; set; }

    public int SoldQuantity { get; set; }

    [NotMapped]
    public int RemainingQuantity => Quantity - SoldQuantity;

    public string Condition { get; set; } = null!;

    public string? ConditionNotes { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Length { get; set; }

    public decimal? Width { get; set; }

    public decimal? Height { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime ListingEndDate { get; set; }

    public bool IsUrgent { get; set; }

    public int Views { get; set; }

    public int Saves { get; set; }

    public string Status { get; set; } = null!;

    public bool AdminApproved { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

   

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<SavedProduct> SavedProducts { get; set; } = new List<SavedProduct>();

    public virtual Shop Shop { get; set; } = null!;
}
